namespace Meeting_Of_Minutes.Models
{
    public class ProfileUpdateRequestModel
    {
        public int ProfileUpdateRequestID { get; set; }

        public int UserID { get; set; }

        public int? StaffID { get; set; }

        public string CurrentUserName { get; set; } = string.Empty;

        public string CurrentEmail { get; set; } = string.Empty;

        public string CurrentContactNo { get; set; } = string.Empty;

        public string CurrentCity { get; set; } = string.Empty;

        public string RequestedUserName { get; set; } = string.Empty;

        public string RequestedEmail { get; set; } = string.Empty;

        public string RequestedContactNo { get; set; } = string.Empty;

        public string RequestedCity { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string DepartmentName { get; set; } = string.Empty;

        public string DocumentPath { get; set; } = string.Empty;

        public string RequestStatus { get; set; } = string.Empty;

        public string AdminRemarks { get; set; } = string.Empty;

        public DateTime Created { get; set; }

        public DateTime? DecisionDate { get; set; }
    }
}
