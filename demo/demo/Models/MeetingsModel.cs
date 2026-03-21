using System.ComponentModel.DataAnnotations;

namespace demo.Models
{
    public class MeetingsModel
    {
        [Key]
        [Required]
        public int MeetingID { get; set; }

        public string MeetingName { get; set; }

        [Required(ErrorMessage = "Meeting Date required !")]
        public DateTime MeetingDate { get; set; }


        [Required]
        public int MeetingVenueID { get; set; }

        public string MeetingVenueName { get; set; }

        public string MeetingTypeName { get; set; }

        [Required]
        public int MeetingTypeID { get; set; }

        public string DepartmentName { get; set; }

        [Required]
        public int DepartmentID {  get; set; }


        [Required]
        public string MeetingDescription { get; set; }


        [Required]
        public string DocumentPath { get; set; }


        [Required]
        public DateTime Created { get; set; }


        [Required]
        public DateTime Modified { get; set; }


        [Required]
        public bool IsCancelled { get; set; }


        [Required]
        public DateTime ?CancellationDateTime { get; set; }


        [Required]
        public string ?CancellationReason { get; set; }

    }
}
