using BTH7.Data;
using BTH7.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BTH7.Services;

public sealed class DynamicPermissionRequirement : IAuthorizationRequirement
{
    public DynamicPermissionRequirement(string policyName)
    {
        PolicyName = policyName;
    }

    public string PolicyName { get; }
}

public sealed class DynamicPermissionPolicyProvider
    : DefaultAuthorizationPolicyProvider
{
    public DynamicPermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options)
    {
    }

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith("Permission:", StringComparison.OrdinalIgnoreCase))
        {
            return base.GetPolicyAsync(policyName);
        }

        var policy = new AuthorizationPolicyBuilder();
        policy.AddRequirements(new DynamicPermissionRequirement(policyName));
        return Task.FromResult<AuthorizationPolicy?>(policy.Build());
    }
}

public sealed class DynamicPermissionAuthorizationHandler
    : AuthorizationHandler<DynamicPermissionRequirement>
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public DynamicPermissionAuthorizationHandler(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        DynamicPermissionRequirement requirement)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return;
        }

        if (user.IsInRole(AppRoles.Administrator))
        {
            context.Succeed(requirement);
            return;
        }

        var identityUser = await _userManager.GetUserAsync(user);
        if (identityUser is null)
        {
            return;
        }

        var roleNames = await _userManager.GetRolesAsync(identityUser);
        if (roleNames.Count == 0)
        {
            return;
        }

        var roleIds = new List<string>();
        foreach (var roleName in roleNames)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role is not null)
            {
                roleIds.Add(role.Id);
            }
        }

        var policyName = requirement.PolicyName["Permission:".Length..];
        var separator = policyName.LastIndexOf('.');
        if (separator <= 0)
        {
            return;
        }

        var controller = policyName[..separator];
        var action = policyName[(separator + 1)..];
        var granted = await _context.RolePermissions
            .AsNoTracking()
            .AnyAsync(rolePermission =>
                roleIds.Contains(rolePermission.RoleId) &&
                rolePermission.Permission.Controller == controller &&
                rolePermission.Permission.Action == action);

        if (granted)
        {
            context.Succeed(requirement);
        }
    }
}

public sealed class DynamicPermissionAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly IAuthorizationService _authorizationService;

    public DynamicPermissionAuthorizationFilter(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor.EndpointMetadata
            .OfType<IAllowAnonymous>()
            .Any())
        {
            return;
        }

        var routeValues = context.RouteData.Values;
        if (!routeValues.TryGetValue("controller", out var controllerValue) ||
            !routeValues.TryGetValue("action", out var actionValue))
        {
            return;
        }

        var controller = controllerValue?.ToString();
        var action = actionValue?.ToString();
        if (string.IsNullOrWhiteSpace(controller) ||
            string.IsNullOrWhiteSpace(action) ||
            controller.Equals("Account", StringComparison.OrdinalIgnoreCase) ||
            controller.Equals("Home", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var result = await _authorizationService.AuthorizeAsync(
            context.HttpContext.User,
            null,
            $"Permission:{controller}.{action}");

        if (!result.Succeeded)
        {
            context.Result = context.HttpContext.User.Identity?.IsAuthenticated == true
                ? new ForbidResult()
                : new ChallengeResult();
        }
    }
}
