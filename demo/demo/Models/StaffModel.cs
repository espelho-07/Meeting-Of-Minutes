using System.ComponentModel.DataAnnotations;

namespace demo.Models
{
    public class StaffModel
    {
        [Key]
        public int? StaffID { get; set; }


        [Required]
        public int DepartmentID {  get; set; }

        public string ?DepartmentName  { get; set; }

        [Required(ErrorMessage = "Staff name required !")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Staffname length must be between 3 and 50 characters.")]
        public string StaffName { get; set; }


        [Required(ErrorMessage = "Mobile number required !")]
        public string MobileNo { get; set; }


        [Required(ErrorMessage = "Email address required !")]
        public string EmailAddress { get; set; }


        [Required]
        public string? Remarks { get; set; }


        public DateTime? Created { get; set; }


        public DateTime? Modified { get; set; }

    }
}
