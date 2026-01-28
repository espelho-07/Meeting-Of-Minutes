using System.ComponentModel.DataAnnotations;

namespace Meeting_Of_Minutes.Models
{
    public class MeetingTypeModel
    {
        [Required]
        public int MeetingTypeID { get; set; }

        [Required(ErrorMessage ="Meeting Type Required ")]
        public string MeetingTypeName { get; set; }

        public string Remarks { get; set; }

        public DateTime Created { get; set; }

        public DateTime Modified { get; set; }
    }
}
