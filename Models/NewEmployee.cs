using System;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace FYP.Models
{
    public class NewEmployee
    {
        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "Please enter Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "New password is required.")]
        [DataType(DataType.Password)]
        [Compare("EmpPw", ErrorMessage = "Passwords do not match.")]
        [StringLength(20, MinimumLength = 7, ErrorMessage = "Password must be 7-20 characters")]
        public string NewPw { get; set; }

        [DataType(DataType.Password)]
        [StringLength(20, MinimumLength = 7, ErrorMessage = "Password must be 7-20 characters")]
        [Required(ErrorMessage = "New password is required.")]
        public string EmpPw { get; set; }

        public int RolesId { get; set; }

        [Required(ErrorMessage = "Please enter Phone Number")]
        [RegularExpression("\\d{8}", ErrorMessage = "8 numbers only")]
        public int PhoneNo { get; set; }

        [Required(ErrorMessage = "Please enter Email")]
        [RegularExpression("(\\d{3}@outlook.com|\\d{5}@outlook.com|\\d{6}@outlook.com)", ErrorMessage = "Invalid Email")]
        public string Email { get; set; }

        public int Tickets { get; set; }

        public DateTime? LastLogin { get; set; }

        public int ClosedTickets { get; set; }

        public string? AccStatus { get; set; }

        // Navigation property for Roles table
        public Roles Roles { get; set; }
    }
}
