using BTH7.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BTH7.Data;

public static class IdentitySeeder
{
    private static readonly string[] Roles =
    [
        AppRoles.Administrator,
        AppRoles.Manager,
        AppRoles.Lecturer,
        AppRoles.Employee,
        AppRoles.Student
    ];

    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        using var scope = services.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("BTH7.Data.IdentitySeeder");

        foreach (var roleName in Roles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            EnsureSucceeded(result, $"create role '{roleName}'");
        }

        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var permissions = new[]
        {
            ("Student", "Index"), ("Student", "Details"), ("Student", "Create"), ("Student", "Edit"), ("Student", "Delete"),
            ("Faculty", "Index"), ("Faculty", "Details"), ("Faculty", "Create"), ("Faculty", "Edit"), ("Faculty", "Delete"),
            ("Lecturer", "Index"), ("Lecturer", "Details"), ("Lecturer", "Create"), ("Lecturer", "Edit"), ("Lecturer", "Delete"),
            ("Employee", "Index"), ("Employee", "Details"), ("Employee", "Create"), ("Employee", "Edit"), ("Employee", "Delete"),
            ("Person", "Index"), ("Person", "Create"), ("Person", "Edit"), ("Person", "Delete"),
            ("UserManagement", "Index"), ("UserManagement", "EditRoles"), ("UserManagement", "Unlock"),
            ("RoleManagement", "Index"), ("RoleManagement", "Details"), ("RoleManagement", "Create"), ("RoleManagement", "Edit"), ("RoleManagement", "Delete"),
            ("PermissionManagement", "Index"), ("PermissionManagement", "Update")
        };

        foreach (var (controller, action) in permissions)
        {
            var permission = await context.DynamicPermissions
                .SingleOrDefaultAsync(item =>
                    item.Controller == controller &&
                    item.Action == action);
            if (permission is null)
            {
                context.DynamicPermissions.Add(new DynamicPermission
                {
                    Controller = controller,
                    Action = action,
                    Description = $"{controller}.{action}"
                });
            }
        }
        await context.SaveChangesAsync();

        var allPermissions = await context.DynamicPermissions.ToListAsync();
        var rolePermissions = new Dictionary<string, Func<string, string, bool>>(StringComparer.Ordinal)
        {
            [AppRoles.Administrator] = (_, _) => true,
            [AppRoles.Manager] = (controller, _) =>
                controller is "Student" or "Faculty" or "Lecturer" or "Employee" or "Person",
            [AppRoles.Lecturer] = (controller, action) =>
                (controller == "Student" && action is "Index" or "Details") ||
                (controller == "Lecturer" && action is "Index" or "Details"),
            [AppRoles.Employee] = (controller, action) =>
                controller == "Employee" && action is "Index" or "Details",
            [AppRoles.Student] = (_, _) => false
        };

        foreach (var (roleName, include) in rolePermissions)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role is null)
            {
                continue;
            }

            foreach (var permission in allPermissions.Where(permission =>
                         include(permission.Controller, permission.Action)))
            {
                var exists = await context.RolePermissions.AnyAsync(rolePermission =>
                    rolePermission.RoleId == role.Id &&
                    rolePermission.PermissionId == permission.Id);
                if (!exists)
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permission.Id
                    });
                }
            }
        }
        await context.SaveChangesAsync();

        if (!configuration.GetValue<bool>("IdentitySeed:SeedDefaultUsers"))
        {
            return;
        }

        var accounts = configuration
            .GetSection("IdentitySeed:Accounts")
            .Get<List<SeedAccount>>();
        if (accounts is null || accounts.Count == 0)
        {
            throw new InvalidOperationException(
                "IdentitySeed:Accounts must contain at least one account when SeedDefaultUsers is enabled.");
        }

        var developmentPassword = environment.IsDevelopment()
            ? configuration["IdentitySeed:DevelopmentPassword"]
            : null;

        foreach (var account in accounts)
        {
            if (string.IsNullOrWhiteSpace(account.Email) ||
                string.IsNullOrWhiteSpace(account.Role) ||
                !Roles.Contains(account.Role, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "Every identity seed account must have an email and one of the configured application roles.");
            }

            var password = account.Password ?? developmentPassword;
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    $"A password must be configured for seed account '{account.Email}'.");
            }

            var user = await userManager.FindByEmailAsync(account.Email);
            if (user is null)
            {
                user = new IdentityUser
                {
                    UserName = account.Email,
                    Email = account.Email,
                    EmailConfirmed = true,
                    LockoutEnabled = true
                };
                var createResult = await userManager.CreateAsync(user, password);
                EnsureSucceeded(createResult, $"create seed account '{account.Email}'");
                logger.LogInformation("Created seed account {Email} for role {Role}.", account.Email, account.Role);
            }
            else if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                var confirmationResult = await userManager.UpdateAsync(user);
                EnsureSucceeded(confirmationResult, $"confirm seed account '{account.Email}'");
            }

            if (!await userManager.IsInRoleAsync(user, account.Role))
            {
                var addRoleResult = await userManager.AddToRoleAsync(user, account.Role);
                EnsureSucceeded(addRoleResult, $"assign role '{account.Role}' to '{account.Email}'");
            }
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"Could not {operation}: {errors}");
    }

    private sealed class SeedAccount
    {
        public string? Email { get; set; }
        public string? Role { get; set; }
        public string? Password { get; set; }
    }
}
