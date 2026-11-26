using BTH7.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTH7.Controllers;

[Authorize]
public class UserManagementController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserManagementController(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users
            .OrderBy(user => user.Email)
            .Select(user => new ManagedUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                LockoutEnd = user.LockoutEnd
            })
            .ToListAsync();

        var now = DateTimeOffset.UtcNow;
        foreach (var user in users)
        {
            user.IsLockedOut = user.LockoutEnd > now;
            var identityUser = await _userManager.FindByIdAsync(user.Id);
            if (identityUser is not null)
            {
                user.Roles = (await _userManager.GetRolesAsync(identityUser)).ToArray();
            }
        }

        return View(new UserManagementViewModel { Users = users });
    }

    [HttpGet]
    public async Task<IActionResult> EditRoles(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var model = await BuildUserRoleEditViewModelAsync(user);
        return IsAjaxRequest()
            ? PartialView("_EditRoles", model)
            : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRoles(string id, string[] selectedRoleIds)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var desiredRoleIds = (selectedRoleIds ?? [])
            .Where(roleId => !string.IsNullOrWhiteSpace(roleId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var desiredRoles = new List<IdentityRole>();
        foreach (var roleId in desiredRoleIds.Distinct(StringComparer.Ordinal))
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role is null)
            {
                ModelState.AddModelError(string.Empty, "Có Role không hợp lệ trong yêu cầu.");
                var invalidModel = await BuildUserRoleEditViewModelAsync(user);
                invalidModel.SelectedRoleIds = desiredRoleIds;
                foreach (var roleOption in invalidModel.Roles)
                {
                    roleOption.Selected = desiredRoleIds.Contains(roleOption.Id, StringComparer.Ordinal);
                }
                return IsAjaxRequest()
                    ? PartialView("_EditRoles", invalidModel)
                    : View("EditRoles", invalidModel);
            }

            desiredRoles.Add(role);
        }

        var currentRoleNames = await _userManager.GetRolesAsync(user);
        var desiredRoleNames = desiredRoles
            .Select(role => role.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToList();

        var removingAdministrator = currentRoleNames.Contains(
            AppRoles.Administrator,
            StringComparer.OrdinalIgnoreCase)
            && !desiredRoleNames.Contains(AppRoles.Administrator, StringComparer.OrdinalIgnoreCase);

        if (removingAdministrator)
        {
            var administrators = await _userManager.GetUsersInRoleAsync(AppRoles.Administrator);
            if (administrators.Count <= 1)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Không thể thu hồi toàn bộ Role Administrator vì hệ thống cần ít nhất một tài khoản Administrator.");
                var protectedModel = await BuildUserRoleEditViewModelAsync(user);
                protectedModel.SelectedRoleIds = desiredRoleIds;
                foreach (var roleOption in protectedModel.Roles)
                {
                    roleOption.Selected = desiredRoleIds.Contains(roleOption.Id, StringComparer.Ordinal);
                }
                return IsAjaxRequest()
                    ? PartialView("_EditRoles", protectedModel)
                    : View("EditRoles", protectedModel);
            }
        }

        var rolesToAdd = desiredRoleNames
            .Except(currentRoleNames, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var rolesToRemove = currentRoleNames
            .Except(desiredRoleNames, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (rolesToAdd.Length > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
            if (!addResult.Succeeded)
            {
                AddIdentityErrors(addResult);
                return await InvalidRoleUpdateResultAsync(user, desiredRoleIds, IsAjaxRequest());
            }
        }

        if (rolesToRemove.Length > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                AddIdentityErrors(removeResult);
                return await InvalidRoleUpdateResultAsync(user, desiredRoleIds, IsAjaxRequest());
            }
        }

        await _userManager.UpdateSecurityStampAsync(user);
        return IsAjaxRequest()
            ? Json(new { success = true })
            : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var lockoutResult = await _userManager.SetLockoutEndDateAsync(user, null);
        var resetResult = await _userManager.ResetAccessFailedCountAsync(user);
        if (!lockoutResult.Succeeded || !resetResult.Succeeded)
        {
            var errors = lockoutResult.Errors
                .Concat(resetResult.Errors)
                .Select(error => error.Description);
            return Problem(
                detail: string.Join(" ", errors),
                statusCode: StatusCodes.Status500InternalServerError);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<UserRoleEditViewModel> BuildUserRoleEditViewModelAsync(IdentityUser user)
    {
        var selectedRoles = await _userManager.GetRolesAsync(user);
        var roles = await _roleManager.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .ToListAsync();

        var roleOptions = roles.Select(role => new RoleCheckboxViewModel
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            Selected = selectedRoles.Contains(role.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
        }).ToList();

        return new UserRoleEditViewModel
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            Roles = roleOptions,
            SelectedRoleIds = roleOptions
                .Where(role => role.Selected)
                .Select(role => role.Id)
                .ToArray()
        };
    }

    private async Task<IActionResult> InvalidRoleUpdateResultAsync(
        IdentityUser user,
        string[] desiredRoleIds,
        bool isAjax)
    {
        var model = await BuildUserRoleEditViewModelAsync(user);
        model.SelectedRoleIds = desiredRoleIds;
        foreach (var roleOption in model.Roles)
        {
            roleOption.Selected = desiredRoleIds.Contains(roleOption.Id, StringComparer.Ordinal);
        }

        return isAjax
            ? PartialView("_EditRoles", model)
            : View("EditRoles", model);
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    private bool IsAjaxRequest()
    {
        return string.Equals(
            Request.Headers["X-Requested-With"],
            "XMLHttpRequest",
            StringComparison.OrdinalIgnoreCase);
    }
}
