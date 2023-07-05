namespace FYP.Models
{
    public class Employee
    {
        public int EmployeeId { get; set; }

        public int RolesId { get; set; }
        public string Role { get; set; } = null!;

        public string Name { get; set; } = null!;

        public string Email { get; set; } = null!;

        public int Phone_no { get; set; }

        public int no_tickets { get; set; }

        public int closed_tickets { get; set; }
    }
}
