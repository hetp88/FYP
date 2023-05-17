using System.ComponentModel.DataAnnotations;

namespace FYP.Models
{
    public class UserLogin
    {
        [Required(ErrorMessage = "Please enter User ID")]
        public string UserID { get; set; } = null!;

        [Required(ErrorMessage = "Please enter Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;
    }

}
