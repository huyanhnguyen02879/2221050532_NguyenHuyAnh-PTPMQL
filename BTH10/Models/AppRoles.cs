namespace BTH7.Models;

public static class AppRoles
{
    public const string Administrator = "Administrator";
    public const string Manager = "Manager";
    public const string Lecturer = "Lecturer";
    public const string Employee = "Employee";
    public const string Student = "Student";
}

public class UserManagementViewModel
{
    public IReadOnlyList<ManagedUserViewModel> Users { get; set; } = [];
}

public class ManagedUserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool IsLockedOut { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = [];
}

public class UserRoleEditViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IReadOnlyList<RoleCheckboxViewModel> Roles { get; set; } = [];
    public string[] SelectedRoleIds { get; set; } = [];
}

public class RoleCheckboxViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Selected { get; set; }
}
