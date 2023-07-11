using System.ComponentModel.DataAnnotations;

namespace FYP.Models
{
    public class VerifyEmail
    {
        [Required(ErrorMessage = "Please enter ypur email")]
        public string Email { get; set; } = null!;
    }
}
