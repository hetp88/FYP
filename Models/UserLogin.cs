using System.ComponentModel.DataAnnotations;

namespace FYP.Models
{
    public class UserLogin
    {
        [Required(ErrorMessage = "Please enter User ID")]
        [RegularExpression("(\\d{8}|\\d{4}|\\d{5}|\\d{3})", ErrorMessage = "either 2,3,5 or 8 digits only")]
        public string UserID { get; set; } = null!;

        [Required(ErrorMessage = "Please enter Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;
        public bool RememberMe { get; set; }
        public bool RedirectToUsers { get; set; }
    }

}
