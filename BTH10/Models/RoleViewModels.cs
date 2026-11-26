using System.ComponentModel.DataAnnotations;

namespace BTH7.Models;

public class RoleListViewModel
{
    public IReadOnlyList<RoleViewModel> Roles { get; set; } = [];
}

public class RoleViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên Role không được để trống.")]
    [StringLength(128, MinimumLength = 2, ErrorMessage = "Tên Role phải từ 2 đến 128 ký tự.")]
    [RegularExpression(@"^[A-Za-zÀ-ỹ0-9 _.-]+$", ErrorMessage = "Tên Role chỉ được chứa chữ, số, khoảng trắng, dấu gạch ngang, dấu gạch dưới và dấu chấm.")]
    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;
    public int UserCount { get; set; }
}
