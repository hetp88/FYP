using System.ComponentModel.DataAnnotations;

namespace FYP.Models
{
    public class NewUser
    {
        [Required(ErrorMessage = "Please enter User ID")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Please enter Password")]
        [StringLength(20, MinimumLength = 8, ErrorMessage = "Password must be 8-20 characters")]
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

        [Required(ErrorMessage = "Please enter Phone Number")]
        [RegularExpression("\\d{8}", ErrorMessage = "8 numbers only")]
        public string PhoneNo { get; set; } = null!;
    }
}
