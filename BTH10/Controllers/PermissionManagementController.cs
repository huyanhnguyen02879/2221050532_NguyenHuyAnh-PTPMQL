using BTH7.Data;
using BTH7.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTH7.Controllers;

[Authorize]
public class PermissionManagementController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<IdentityRole> _roleManager;

    public PermissionManagementController(
        ApplicationDbContext context,
        RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        var roles = await _roleManager.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .Select(role => new PermissionRoleViewModel
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty
            })
            .ToListAsync();

        var permissions = await _context.DynamicPermissions
            .AsNoTracking()
            .Include(permission => permission.RolePermissions)
            .OrderBy(permission => permission.Controller)
            .ThenBy(permission => permission.Action)
            .ToListAsync();

        var controllers = permissions
            .GroupBy(permission => permission.Controller)
            .Select(group => new PermissionControllerViewModel
            {
                Name = group.Key,
                Actions = group.Select(permission => new PermissionActionViewModel
                {
                    Id = permission.Id,
                    Action = permission.Action,
                    Description = permission.Description,
                    GrantedRoleIds = permission.RolePermissions
                        .Select(rolePermission => rolePermission.RoleId)
                        .ToArray()
                }).ToArray()
            })
            .ToArray();

        var model = new PermissionManagementViewModel
        {
            Roles = roles,
            Controllers = controllers
        };

        return IsAjaxRequest()
            ? PartialView("_PermissionMatrix", model)
            : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        string roleId,
        int permissionId,
        bool granted)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role is null)
        {
            return NotFound();
        }

        var permission = await _context.DynamicPermissions.FindAsync(permissionId);
        if (permission is null)
        {
            return NotFound();
        }

        if (string.Equals(role.Name, AppRoles.Administrator, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Quyền của Administrator luôn được giữ.");
        }

        var existing = await _context.RolePermissions.FindAsync(roleId, permissionId);
        if (granted)
        {
            if (existing is null)
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId
                });
                await _context.SaveChangesAsync();
            }
        }
        else if (existing is not null)
        {
            _context.RolePermissions.Remove(existing);
            await _context.SaveChangesAsync();
        }

        return Json(new { success = true });
    }

    private bool IsAjaxRequest()
    {
        return string.Equals(
            Request.Headers["X-Requested-With"],
            "XMLHttpRequest",
            StringComparison.OrdinalIgnoreCase);
    }
}
