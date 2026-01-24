using System.ComponentModel.DataAnnotations;

namespace Meeting_Of_Minutes.Models
{
    public class DepartmentModel
    {
        [Key]
        public int DepartmentID { get; set; }

        [Required(ErrorMessage ="The Field Is Required")]
        [StringLength(50,ErrorMessage ="Department Name Can't Exceed Than 50 Characters")]
        [Display(Name = "Department Name")]
        public string DepartmentName { get; set; }

        public DateTime Created { get; set; }

        public DateTime Modified { get; set; }
    }
}
