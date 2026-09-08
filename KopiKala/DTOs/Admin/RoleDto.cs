namespace KopiKala.DTOs.Admin;

public class RoleDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsTemplate { get; set; }

    public List<PermissionDto> Permissions { get; set; } = new();
}
