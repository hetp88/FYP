using System.ComponentModel.DataAnnotations;

namespace FYP.Models
{
    public class Newpw
    {
        [DataType(DataType.Password)]
        public string newpw { get; set; } = null!;
        [DataType(DataType.Password)]
        public string cfmpw { get; set; } = null!;

    }
}