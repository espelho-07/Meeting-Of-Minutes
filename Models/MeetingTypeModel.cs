using System.ComponentModel.DataAnnotations;

namespace Meeting_Of_Minutes.Models
{
    public class MeetingTypeModel
    {
        [Required]
        public int MeetingTypeID { get; set; }

        [Required]
        public string MeetingTypeName { get; set; }

        [Required]
        public string Remarks { get; set; }

        [Required]
        public DateTime Created { get; set; }

        [Required]
        public DateTime Modified { get; set; }
    }
}
