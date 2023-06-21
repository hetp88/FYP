using System.ComponentModel.DataAnnotations;

namespace FYP.Models
{
    public class FAQ
    {
        public int FaqId { get; set; }
        public String Category { get; set; } = null!;
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Please enter question")]
        [StringLength(300, ErrorMessage = "Maximum is 300 characters")]
        public string Question { get; set; } = null!;

        [Required(ErrorMessage = "Please enter solution")]
        [StringLength(300, ErrorMessage = "Maximum is 300 characters")]
        public string Solution { get; set; } = null!;
    }
}
