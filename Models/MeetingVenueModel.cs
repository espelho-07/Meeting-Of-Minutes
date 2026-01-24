using System.ComponentModel.DataAnnotations;

namespace Meeting_Of_Minutes.Models
{
    public class MeetingVenueModel
    {
        [Required]
        public int MeetingVenueID { get; set; }

        [Required]
        public string MeetingVenueName { get; set; }

        [Required]
        public DateTime Created { get; set; }

        [Required]
        public DateTime Modified { get; set; }
    }
}
