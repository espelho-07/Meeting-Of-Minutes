using System.ComponentModel.DataAnnotations;

namespace Meeting_Of_Minutes.Models
{
    public class StaffModel
    {
        [Key]
        [Required]
        public int StaffID { get; set; }

        public int DepartmentID { get; set; }

        public string? DepartmentName { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "Staff Name Can't Exceed Than 50 Characters")]
        public string? StaffName { get; set; }

        [Required]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Enter valid 10 digit mobile number")]
        public string? MobileNo { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$", ErrorMessage = "Incorrect email Address Enter Valid Address Format.")]
        public string? EmailAddress { get; set; }

        public string? Remarks { get; set; }

        public DateTime Created { get; set; }

        public DateTime Modified { get; set; }
    }
}

