using System.ComponentModel.DataAnnotations;

namespace FYP.Models
{
    public class EmployeeSchedule
    {
        public int LeaveId { get; set; }

        public int EmployeeId { get; set; }

        [Required(ErrorMessage = "Choose start date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Choose end date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Enter your reason")]
        public string Reason { get; set; } = null!;

        [Required(ErrorMessage = "Upload proof")]
        public string ProofProvided { get; set; }

        public string IsApproved { get; set; } = null!;
    }

}
