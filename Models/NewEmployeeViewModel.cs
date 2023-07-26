using System.ComponentModel.DataAnnotations;

namespace FYP.Models
{
    public class NewEmployeeViewModel
    {
        public int EmployeeId { get; set; }
        [Required(ErrorMessage = "Please select a role")]
        public int RolesId { get; set; }

        [Required(ErrorMessage = "Please enter Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "New password is required.")]
        [DataType(DataType.Password)]
        [StringLength(20, MinimumLength = 7, ErrorMessage = "Password must be 7-20 characters")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Please enter Phone Number")]
        [RegularExpression("\\d{8}", ErrorMessage = "8 numbers only")]
        public int PhoneNo { get; set; }

        [Required(ErrorMessage = "Please enter Email")]
        [RegularExpression("(\\d{3}@outlook.com|\\d{5}@outlook.com|\\d{6}@outlook.com)", ErrorMessage = "Invalid Email")]
        public string Email { get; set; }
    }
}
