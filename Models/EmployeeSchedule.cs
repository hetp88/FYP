using System.ComponentModel.DataAnnotations;

namespace FYP.Models
{
    public class EmployeeSchedule
    {
        public int LeaveId { get; set; }
        public int EmployeeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; }
        public bool ProofProvided { get; set; }
        public string IsApproved { get; set; }
    }

}
