namespace FYP.Models
{
    public class Users
    {
        public int userId { get; set; }
        public string username { get; set; } = null!;
        public int RolesId { get; set; }
        public string role { get; set; } 
        public string school { get; set; } = null!;
        public string email { get; set; } = null!;
        public int phoneNo { get; set; }
        public DateTime last_login { get; set; }
    }
}
