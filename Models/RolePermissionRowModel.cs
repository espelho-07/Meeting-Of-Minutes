namespace Meeting_Of_Minutes.Models
{
    public class RolePermissionRowModel
    {
        public string PermissionKey { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, bool> RoleAccess { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
