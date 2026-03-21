using System.ComponentModel.DataAnnotations;

namespace demo.Models
{
    public class MeetingVenueModel
    {
        [Key]
        public int MeetingVenueID { get; set; }


        [Required(ErrorMessage = "Meeting Venue Name required !")]
        [StringLength(100, ErrorMessage = "Can't exceed length of 100 !")]
        public string MeetingVenueName { get; set; }

        public int ?MeetingCount { get; set; }

        [Required]
        public DateTime Created { get; set; }


        [Required]
        public DateTime Modified { get; set; }
    }
}
