namespace Meeting_Of_Minutes.Models
{
    public class UserManagementModel
    {
        public int UserID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactNo { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public int? DepartmentID { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int? StaffID { get; set; }
        public bool IsAutoPassword { get; set; }
        public int? ManagedByAdminUserID { get; set; }
        public string ManagedByAdminName { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
    }
}
