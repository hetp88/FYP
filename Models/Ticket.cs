using System.ComponentModel.DataAnnotations;

namespace FYP.Models
{
    public class Ticket
    {
        [Required(ErrorMessage = "Enter User ID")]
        [RegularExpression("(\\d{8}|\\d{4})", ErrorMessage = "Either 4 digits (staff) or 8 digits (student)")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Please enter description")]
        [StringLength(250, ErrorMessage = "Maximum is 250 characters")]
        public string Description { get; set; } = null!;

        [Required(ErrorMessage = "Please select category")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Please select Status")]
        public int StatusId { get; set; }

        [Required(ErrorMessage = "Please select date and time")]
        public DateTime DateTime { get; set; }

        [Required(ErrorMessage = "Please select Status")]
        public int PriorityId { get; set; }

        [Required(ErrorMessage = "Please select Status")]
        public int EmployeeId { get; set; }

        public string? DevicesInvolved { get; set; } = null;

        public string? Additional_Details { get; set; } = null;

        public string Resolution { get; set; } = null!;
    }
}
