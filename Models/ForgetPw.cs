using System.ComponentModel.DataAnnotations;

namespace FYP.Models
{
    public class ForgetPw
    {
        [Required(ErrorMessage = "Please enter ypur email")]
        public string Email { get; set; } = null!;
        //[DataType(DataType.Password)]
        //public string NewPassword { get; set; } = null!;
        //[DataType(DataType.Password)]
        //public string NewCfmPassword { get; set; } = null!;
    }
}
