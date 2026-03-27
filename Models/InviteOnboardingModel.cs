using System.ComponentModel.DataAnnotations;

namespace Meeting_Of_Minutes.Models
{
    public class InviteOnboardingModel
    {
        [Required]
        public string InviteToken { get; set; } = string.Empty;

        public int UserID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$",
            ErrorMessage = "Password must include uppercase, lowercase, number, and special character.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm password")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
