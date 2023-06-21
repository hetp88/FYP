using System.ComponentModel.DataAnnotations;

namespace FYP.Models
{
    public class EmployeeSchedule
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string reason { get; set; } = null!;
        public int employee_id { get; set; }
        public bool is_approved { get; set; }
    }

}
