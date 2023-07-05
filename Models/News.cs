using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace FYP.Models
{
    public class News
    {
        public int newsID { get; set; }
        public string newsUpdate { get; set; } = null!;
    }
}
