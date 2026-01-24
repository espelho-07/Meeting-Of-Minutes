using System.ComponentModel.DataAnnotations;

namespace Meeting_Of_Minutes.Models
{
    public class MeetingsModel
    {
        [Required]
        public int MeetingID { get; set; }

        [Required(ErrorMessage = "The Field Is Required")]
        public DateTime MeetingDate { get; set; }

        [Required]
        public int MeetingVenueID { get; set; }

        [Required]
        public int MeetingTypeID { get; set; }

        [Required]
        public int DepartmentID { get; set; }

        [Required]
        public string? MeetingDescription { get; set; }

        [Required]
        public string? DocumentPath { get; set; }

        [Required]
        public DateTime Created { get; set; }

        [Required]
        public DateTime Modified { get; set; }

        [Required]
        public bool? IsCancelled { get; set; }

        [Required]
        public DateTime? CancellationDateTime { get; set; }

        [Required]
        public string? CancellationReason { get; set; }

    }
}
