namespace AdminMembers.Models
{
    public class Role
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Permission Permission { get; set; } = Permission.Read;

        // Navigation property
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }

    public enum Permission
    {
        Read = 1,
        Write = 2,
        ReadWrite = 3
    }
}
