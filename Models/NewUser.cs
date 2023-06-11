using System.ComponentModel.DataAnnotations;

namespace FYP.Models
{
    public class NewUser
    {
        //[Required(ErrorMessage = "Please enter User ID")]
        //[RegularExpression("(\\d{8}|\\d{4}|\\d{5}|\\d{3})", ErrorMessage = "either 2,3,5 or 8 digits only")]
        //public int UserID { get; set; }

        [Required(ErrorMessage = "Please enter Password")]
        [StringLength(20, MinimumLength = 7, ErrorMessage = "Password must be 7-20 characters")]
        [DataType(DataType.Password)]
        public string UserPw { get; set; } = null!;

        [Compare("UserPw", ErrorMessage = "Passwords do not match")]
        [DataType(DataType.Password)]
        public string UserPw2 { get; set; } = null!;

        [Required(ErrorMessage = "Please enter Full Name")]
        public string UserName { get; set; } = null!;

        //[Required(ErrorMessage = "Please enter Full Name")]
        //public int Role { get; set; }

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
