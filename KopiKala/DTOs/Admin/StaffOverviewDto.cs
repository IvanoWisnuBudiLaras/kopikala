namespace KopiKala.DTOs.Admin;

public class StaffOverviewDto
{
    public Guid UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public List<string> Permissions { get; set; } = new();

    public DateTime CreatedAt { get; set; }
}
