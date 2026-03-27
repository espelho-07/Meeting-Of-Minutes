using System.ComponentModel.DataAnnotations;

namespace Meeting_Of_Minutes.Models
{
    public class UserModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter valid email address")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Please select role")]
        public string? UserRole { get; set; }

        public string? CompanyName { get; set; }

        public int? DepartmentID { get; set; }
    }
}
