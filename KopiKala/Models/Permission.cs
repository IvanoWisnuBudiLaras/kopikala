namespace KopiKala.Models;

public partial class Permission
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string GroupName { get; set; } = null!;

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
