using System.ComponentModel.DataAnnotations;

namespace demo.Models
{
    public class MeetingTypeModel
    {
        [Key]
        public int MeetingTypeID { get; set; }


        [Required(ErrorMessage = "Meeting Type Name required !")]
        [StringLength(100,ErrorMessage = "Can't exceed length of 100 !")]
        public string MeetingTypeName { get; set; }


        [Required(ErrorMessage = "Remarks required !")]
        [StringLength(100, ErrorMessage = "Can't exceed length of 100 !")]
        public string Remarks {  get; set; }

        [Required]
        public DateTime Created {  get; set; }


        [Required]
        public DateTime Modified { get; set; }
    }
}
