using System.ComponentModel.DataAnnotations;

namespace FYP.Models
{
    public class NewEmployee
    {
        public int employee_id { get; set; }
        public string name { get; set; } = null!;
        [Required(ErrorMessage = "Please enter Password")]
        [StringLength(20, MinimumLength = 7, ErrorMessage = "Password must be 7-20 characters")]
        [DataType(DataType.Password)]
        public string EmpPw { get; set; } = null!;
        public string roles_id { get; set; } = null!;
        [Required(ErrorMessage = "Please enter Phone Number")]
        [RegularExpression("\\d{8}", ErrorMessage = "8 numbers only")]
        public int phone_no { get; set; }
        [Required(ErrorMessage = "Please enter Email")]
        [RegularExpression("(\\@|\\com.sg)", ErrorMessage = "Invalid Email")]
        public string email { get; set; } = null!;
    }
}
