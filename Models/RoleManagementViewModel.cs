namespace Meeting_Of_Minutes.Models
{
    public class RoleManagementViewModel
    {
        public List<string> Roles { get; set; } = new();
        public List<RolePermissionRowModel> Permissions { get; set; } = new();
    }
}
