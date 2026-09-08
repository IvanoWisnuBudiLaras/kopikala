using System.ComponentModel.DataAnnotations;

namespace KopiKala.DTOs.Admin;

public class ManageRoleDto
{
    public int? RoleId { get; set; }

    [Required(ErrorMessage = "Nama peran wajib diisi.")]
    [StringLength(50, ErrorMessage = "Nama peran maksimal 50 karakter.")]
    public string RoleName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Deskripsi maksimal 200 karakter.")]
    public string? Description { get; set; }

    public bool IsTemplate { get; set; }

    public List<int> SelectedPermissionIds { get; set; } = new();
}
