using System.ComponentModel.DataAnnotations;

namespace Meeting_Of_Minutes.Models
{
    public class MeetingsModel
    {
        [Key]
        public int MeetingID { get; set; }

        [Required(ErrorMessage = "The Field Is Required")]
        public DateTime MeetingDate { get; set; }

        public int MeetingVenueID { get; set; }

        public int MeetingTypeID { get; set; }

        public int DepartmentID { get; set; }

        public string? MeetingDescription { get; set; }

        public string? DocumentPath { get; set; }

        public DateTime Created { get; set; }

        public DateTime Modified { get; set; }

        [Required]
        public bool? IsCancelled { get; set; }

        public DateTime? CancellationDateTime { get; set; }

        [Required]
        public string? CancellationReason { get; set; }

    }
}
