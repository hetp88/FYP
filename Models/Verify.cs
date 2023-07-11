using System.ComponentModel.DataAnnotations;

namespace FYP.Models
{
    public class Verify
    {
        [Required(ErrorMessage = "Verification code is required.")]
        [Display(Name = "Verification Code")]
        public string VerificationCode { get; set; } = null!;
    }
}
