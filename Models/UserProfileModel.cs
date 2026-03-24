namespace Meeting_Of_Minutes.Models
{
    public class UserProfileModel
    {
        public int UserID { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string ContactNo { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string UserRole { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public int? DepartmentID { get; set; }

        public string DepartmentName { get; set; } = string.Empty;

        public bool IsAutoPassword { get; set; }

        public string RequestedUserName { get; set; } = string.Empty;

        public string RequestedEmail { get; set; } = string.Empty;

        public string RequestedContactNo { get; set; } = string.Empty;

        public string RequestedCity { get; set; } = string.Empty;

        public string AdminRemarks { get; set; } = string.Empty;

        public List<ProfileUpdateRequestModel> UpdateRequests { get; set; } = new List<ProfileUpdateRequestModel>();

        public string? CurrentPassword { get; set; }

        public string? NewPassword { get; set; }

        public string? ConfirmPassword { get; set; }
    }
}
