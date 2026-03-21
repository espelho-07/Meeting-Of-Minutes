using System.ComponentModel.DataAnnotations;

namespace demo.Models
{
    public class MeetingMemberModel
    {
        [Key]
        public int ?MeetingMemberId { get; set; }

        public string ?MeetingName { get; set; }

        [Required]
        public int MeetingID { get; set; }


        public string ?StaffName { get; set; }

        [Required]
        public int StaffID { get; set; }


        public string ?IsPresent { get; set; }


        [Required]
        public string ?Remarks { get; set; }

        [Required]
        public DateTime Created { get; set; }

        [Required]
        public DateTime Modified { get; set; }
    }
}
