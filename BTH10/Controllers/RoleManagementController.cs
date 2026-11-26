using BTH7.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BTH7.Controllers;

[Authorize]
public class RoleManagementController : Controller
{
    private const int PageSize = 10;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<IdentityUser> _userManager;

    public RoleManagementController(
        RoleManager<IdentityRole> roleManager,
        UserManager<IdentityUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? searchTerm, int page = 1)
    {
        var rolesQuery = _roleManager.Roles.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            rolesQuery = rolesQuery.Where(role =>
                role.Name != null && role.Name.Contains(term));
        }

        var totalCount = await rolesQuery.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var roles = await rolesQuery
            .OrderBy(role => role.Name)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        var models = new List<RoleViewModel>(roles.Count);
        foreach (var role in roles)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name ?? string.Empty);
            models.Add(new RoleViewModel
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                NormalizedName = role.NormalizedName ?? string.Empty,
                UserCount = usersInRole.Count
            });
        }

        ViewData["SearchTerm"] = searchTerm?.Trim() ?? string.Empty;
        ViewData["Page"] = page;
        ViewData["TotalPages"] = totalPages;

        return IsAjaxRequest()
            ? PartialView("_RoleTable", models)
            : View(new RoleListViewModel { Roles = models });
    }

    public async Task<IActionResult> Details(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        var model = await BuildRoleViewModelAsync(role);
        return IsAjaxRequest()
            ? PartialView("_RoleDetails", model)
            : View(model);
    }

    public IActionResult Create()
    {
        var model = new RoleViewModel();
        ViewData["FormAction"] = nameof(Create);
        ViewData["ModalTitle"] = "Thêm Role";
        return IsAjaxRequest()
            ? FormPartial(model, nameof(Create))
            : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return IsAjaxRequest()
                ? FormPartial(model, nameof(Create))
                : View(model);
        }

        var roleName = model.Name.Trim();
        if (await _roleManager.RoleExistsAsync(roleName))
        {
            ModelState.AddModelError(nameof(model.Name), "Role này đã tồn tại.");
            return IsAjaxRequest()
                ? FormPartial(model, nameof(Create))
                : View(model);
        }

        var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return IsAjaxRequest()
                ? FormPartial(model, nameof(Create))
                : View(model);
        }

        return IsAjaxRequest()
            ? Json(new { success = true })
            : RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        if (string.Equals(role.Name, AppRoles.Administrator, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Không thể đổi tên Role Administrator.");
        }

        var model = await BuildRoleViewModelAsync(role);
        ViewData["FormAction"] = nameof(Edit);
        ViewData["ModalTitle"] = "Cập nhật Role";
        return IsAjaxRequest()
            ? FormPartial(model, nameof(Edit))
            : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, RoleViewModel model)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        if (string.Equals(role.Name, AppRoles.Administrator, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Không thể đổi tên Role Administrator.");
        }

        if (!ModelState.IsValid)
        {
            model.Id = id;
            return IsAjaxRequest()
                ? FormPartial(model, nameof(Edit))
                : View(model);
        }

        var roleName = model.Name.Trim();
        var existingRole = await _roleManager.FindByNameAsync(roleName);
        if (existingRole is not null &&
            !string.Equals(existingRole.Id, role.Id, StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(model.Name), "Role này đã tồn tại.");
            model.Id = id;
            return IsAjaxRequest()
                ? FormPartial(model, nameof(Edit))
                : View(model);
        }

        role.Name = roleName;
        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            model.Id = id;
            return IsAjaxRequest()
                ? FormPartial(model, nameof(Edit))
                : View(model);
        }

        return IsAjaxRequest()
            ? Json(new { success = true })
            : RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        var model = await BuildRoleViewModelAsync(role);
        return IsAjaxRequest()
            ? PartialView("_RoleDelete", model)
            : View(model);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        if (string.Equals(role.Name, AppRoles.Administrator, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Không thể xóa Role Administrator.");
            var protectedModel = await BuildRoleViewModelAsync(role);
            return IsAjaxRequest()
                ? PartialView("_RoleDelete", protectedModel)
                : View("Delete", protectedModel);
        }

        var users = await _userManager.GetUsersInRoleAsync(role.Name ?? string.Empty);
        if (users.Count > 0)
        {
            ModelState.AddModelError(string.Empty,
                $"Không thể xóa Role đang được gán cho {users.Count} người dùng.");
            var inUseModel = await BuildRoleViewModelAsync(role);
            return IsAjaxRequest()
                ? PartialView("_RoleDelete", inUseModel)
                : View("Delete", inUseModel);
        }

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            var failedModel = await BuildRoleViewModelAsync(role);
            return IsAjaxRequest()
                ? PartialView("_RoleDelete", failedModel)
                : View("Delete", failedModel);
        }

        return IsAjaxRequest()
            ? Json(new { success = true })
            : RedirectToAction(nameof(Index));
    }

    private async Task<RoleViewModel> BuildRoleViewModelAsync(IdentityRole role)
    {
        var users = await _userManager.GetUsersInRoleAsync(role.Name ?? string.Empty);
        return new RoleViewModel
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            NormalizedName = role.NormalizedName ?? string.Empty,
            UserCount = users.Count
        };
    }

    private PartialViewResult FormPartial(RoleViewModel model, string actionName)
    {
        ViewData["FormAction"] = actionName;
        ViewData["ModalTitle"] = actionName == nameof(Create)
            ? "Thêm Role"
            : "Cập nhật Role";
        return PartialView("_RoleForm", model);
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
