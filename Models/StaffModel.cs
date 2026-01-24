using System.ComponentModel.DataAnnotations;

namespace Meeting_Of_Minutes.Models
{
    public class StaffModel
    {
        [Key]
        [Required]
        public int StaffID { get; set; }

        public int DepartmentID { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Staff Name Can't Exceed Than 50 Characters")]
        public string StaffName { get; set; }

        [Required]
        [RegularExpression(@"^\(?([0-9]{2})[-. ]?([0-9]{4})[-. ]?([0-9]{3})[-. ]?([0-9]{3})$", ErrorMessage = "Not a valid Phone number")]
        public string MobileNo { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$", ErrorMessage = "Incorrect email Address Enter Valid Address Format.")]
        public string EmailAddress { get; set; }

        public string? Remarks { get; set; }

        public DateTime Created { get; set; }

        public DateTime Modified { get; set; }
    }
}
