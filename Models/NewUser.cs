using System.ComponentModel.DataAnnotations;

namespace FYP.Models
{
    public class NewUser
    {

        [Required(ErrorMessage = "Please enter Password")]
        [StringLength(20, MinimumLength = 8, ErrorMessage = "Password must be 8-20 char")]
        [DataType(DataType.Password)]
        public string UserPw { get; set; } = null!;

        [Compare("UserPw", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string UserPw2 { get; set; } = null!;

        [Required(ErrorMessage = "Please enter Full Name")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Please choose school")]
        public string School { get; set; } = null!;

        [Required(ErrorMessage = "Please enter Email")]
        [EmailAddress(ErrorMessage = "Invalid Email")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Please enter Fitness Score")]
        [RegularExpression("\\d[8]", ErrorMessage = "8 numbers only")]
        public int PhoneNo { get; set; }
    }
}
