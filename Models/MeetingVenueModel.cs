using System.ComponentModel.DataAnnotations;

namespace Meeting_Of_Minutes.Models
{
    public class MeetingVenueModel
    {
        public int MeetingVenueID { get; set; }

        [Required(ErrorMessage = "Meeting Venue Required")]
        public string MeetingVenueName { get; set; }

        public DateTime Created { get; set; }

        public DateTime Modified { get; set; }
    }
}
