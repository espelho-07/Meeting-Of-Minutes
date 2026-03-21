using System.ComponentModel.DataAnnotations;

namespace demo.Models
{
    public class DepartmentModel
    {
        [Key]
        public int DepartmentID { get; set; }

        [Required(ErrorMessage ="Department Name is Required !")]
        [Display(Name = "Department Name")]
        public string ?DepartmentName { get; set; }

        public int StaffCount { get; set; }

        public int MeetingCount { get; set; }

        [Required]
        public DateTime Created { get; set; }

        [Required]
        public DateTime Modified { get; set; }

        // Path stored in database
        public string? DepartmentLogo { get; set; }

        // File uploaded from form
        [Display(Name = "Department Logo")]
        public IFormFile? LogoFile { get; set; }
    }
}
