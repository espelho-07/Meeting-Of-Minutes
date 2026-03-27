using System.ComponentModel.DataAnnotations;

namespace Meeting_Of_Minutes.Models
{
    public class ManagedUserCreateModel
    {
        [Required(ErrorMessage = "Username is required")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Enter valid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact number is required")]
        [StringLength(15)]
        public string ContactNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select role")]
        public string UserRole { get; set; } = "User";

        [Required(ErrorMessage = "Please select company")]
        public string CompanyName { get; set; } = string.Empty;

        public int? DepartmentID { get; set; }
        public int? ManagedByAdminUserID { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
