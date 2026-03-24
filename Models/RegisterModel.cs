using System.ComponentModel.DataAnnotations;

namespace Meeting_Of_Minutes.Models
{
    public class RegisterModel
    {
        [Required(ErrorMessage = "Username is required")]
        public string? UserName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter valid email address")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Contact number is required")]
        [StringLength(15, ErrorMessage = "Contact number can be max 15 digits")]
        public string? ContactNo { get; set; }

        [Required(ErrorMessage = "City is required")]
        [StringLength(100, ErrorMessage = "City can be max 100 characters")]
        public string? City { get; set; }

        [Required(ErrorMessage = "Please select role")]
        public string? UserRole { get; set; }

        [Required(ErrorMessage = "Company is required")]
        public string? CompanyName { get; set; }

        public int? DepartmentID { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$",
            ErrorMessage = "Password must be at least 8 characters and include uppercase, lowercase, number, and special character.")]
        public string? Password { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("Password", ErrorMessage = "Password and Confirm Password must match")]
        public string? ConfirmPassword { get; set; }
    }
}
