using System.ComponentModel.DataAnnotations;

namespace FYP.Models
{
    public class EmployeeSchedule
    {
        [Required(ErrorMessage = "Employee ID is required")]
        public int EmployeeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; }
        public bool ProofProvided { get; set; }
        public bool IsApproved { get; set; }
    }

}
