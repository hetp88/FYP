using System.ComponentModel.DataAnnotations;

namespace FYP.Models
{
    public class NewEmployee
    {
        public int Employee_id { get; set; }

        [Required(ErrorMessage = "Please enter Name")]
        public string Name { get; set; } = null!;

        [DataType(DataType.Password)]
        public string EmpPw { get; set; } = null!;

        public int Roles_id { get; set; }

        [Required(ErrorMessage = "Please enter Phone Number")]
        [RegularExpression("\\d{8}", ErrorMessage = "8 numbers only")]
        public int Phone_no { get; set; }

        [Required(ErrorMessage = "Please enter Email")]
        [RegularExpression("(\\d{3}@outlook.com|\\d{5}@outlook.com|\\d{6}@outlook.com)", ErrorMessage = "Invalid Email")]
        public string Email { get; set; } = null!;

        public int Tickets { get; set; }

        public DateTime? Last_login { get; set; }

        public int Closed_Tickets { get; set; }
    }
}