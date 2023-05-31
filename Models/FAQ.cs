using System.ComponentModel.DataAnnotations;

namespace FYP.Models
{
    public class FAQ
    {
        [Required(ErrorMessage = "Please select category")]
        public int FaqId { get; set; }
        public String Category { get; set; }
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Please enter question")]
        [StringLength(300, ErrorMessage = "Maximum is 300 characters")]
        public string Question { get; set; } = null!;

        [Required(ErrorMessage = "Please enter solution")]
        [StringLength(300, ErrorMessage = "Maximum is 300 characters")]
        public string Solution { get; set; } = null!;
    }
}
