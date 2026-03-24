namespace Meeting_Of_Minutes.Models
{
    public class StaffTransferRequestModel
    {
        public int StaffTransferRequestID { get; set; }

        public int StaffID { get; set; }

        public int? UserID { get; set; }

        public string StaffName { get; set; } = string.Empty;

        public string EmailAddress { get; set; } = string.Empty;

        public string MobileNo { get; set; } = string.Empty;

        public string SourceCompanyName { get; set; } = string.Empty;

        public int SourceDepartmentID { get; set; }

        public string SourceDepartmentName { get; set; } = string.Empty;

        public string TargetCompanyName { get; set; } = string.Empty;

        public int TargetDepartmentID { get; set; }

        public string TargetDepartmentName { get; set; } = string.Empty;

        public int RequestedByUserID { get; set; }

        public string RequestedByUserName { get; set; } = string.Empty;

        public string TransferReason { get; set; } = string.Empty;

        public string DocumentPath { get; set; } = string.Empty;

        public string RequestStatus { get; set; } = string.Empty;

        public string AdminRemarks { get; set; } = string.Empty;

        public DateTime Created { get; set; }

        public DateTime? DecisionDate { get; set; }
    }
}
