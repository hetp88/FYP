namespace FYP.Models
{
    public class EmployeeList
    {
        public int EmployeeId { get; set; }
        public int RolesId { get; set; }
        public string EmployeePw { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int PhoneNo { get; set; }

        public int? Tickets { get; set; }
    }

}
