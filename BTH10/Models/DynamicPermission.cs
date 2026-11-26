namespace BTH7.Models;

public class DynamicPermission
{
    public int Id { get; set; }
    public string Controller { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}

public class RolePermission
{
    public string RoleId { get; set; } = string.Empty;
    public Microsoft.AspNetCore.Identity.IdentityRole Role { get; set; } = null!;
    public int PermissionId { get; set; }
    public DynamicPermission Permission { get; set; } = null!;
}

public class PermissionManagementViewModel
{
    public IReadOnlyList<PermissionRoleViewModel> Roles { get; set; } = [];
    public IReadOnlyList<PermissionControllerViewModel> Controllers { get; set; } = [];
}

public class PermissionRoleViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class PermissionControllerViewModel
{
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<PermissionActionViewModel> Actions { get; set; } = [];
}

public class PermissionActionViewModel
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IReadOnlyList<string> GrantedRoleIds { get; set; } = [];
}
