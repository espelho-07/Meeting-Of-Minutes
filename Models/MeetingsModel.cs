using System;
using System.ComponentModel.DataAnnotations;

namespace Meeting_Of_Minutes.Models
{
    public class MeetingsModel
    {
        [Key]
        public int MeetingID { get; set; }

        [Required(ErrorMessage = "Meeting date & time is required")]
        public DateTime? MeetingDate { get; set; }

        [Required(ErrorMessage = "Venue is required")]
        public int? MeetingVenueID { get; set; }

        [Required(ErrorMessage = "Meeting type is required")]
        public int? MeetingTypeID { get; set; }

        [Required(ErrorMessage = "Department is required")]
        public int? DepartmentID { get; set; }

        [MaxLength(250, ErrorMessage = "Description can be max 250 characters")]
        public string? MeetingDescription { get; set; }

        public string? DocumentPath { get; set; }

        public DateTime Created { get; set; } = DateTime.Now;
        public DateTime Modified { get; set; } = DateTime.Now;

        [Required]
        public bool IsCancelled { get; set; } = false;

        public DateTime? CancellationDateTime { get; set; }

        [MaxLength(250)]
        public string? CancellationReason { get; set; }
    }
}
