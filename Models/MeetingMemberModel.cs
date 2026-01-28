using System.ComponentModel.DataAnnotations;

namespace Meeting_Of_Minutes.Models
{
    public class MeetingMemberModel
    {
        [Required]
        public int MeetingMemberID { get; set; }

        [Required]
        public int MeetingID { get; set; }

        [Required]
        public int StaffID { get; set; }

        [Required]
        public bool IsPresent { get; set; }

        public string? Remarks { get; set; }

        public DateTime Created { get; set; }

        public DateTime Modified { get; set; }
    }
}
