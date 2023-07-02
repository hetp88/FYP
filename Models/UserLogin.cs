using System.ComponentModel.DataAnnotations;

namespace FYP.Models
{
    public class UserLogin
    {
        [Required(ErrorMessage = "Please enter User ID")]
        public int UserID { get; set; }

        [Required(ErrorMessage = "Please enter Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;
        public bool RememberMe { get; set; }
        public bool RedirectToUsers { get; set; }
        public DateTime Last_login { get; set; }
    }

}
