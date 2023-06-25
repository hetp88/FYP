﻿using System.ComponentModel.DataAnnotations;

namespace FYP.Models
{
    public class Ticket
    {
        public int TicketId { get; set; }

        //[Required(ErrorMessage = "Enter user ID")]
        //[RegularExpression("(\\d{8}|\\d{4})", ErrorMessage = "Either 4 digits (staff) or 8 digits (student)")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Please select ticket type")]
        public string Type { get; set; } = null!;

        [Required(ErrorMessage = "Please enter description")]
        [StringLength(250, ErrorMessage = "Maximum is 250 characters")]
        public string Description { get; set; } = null!;

        [Required(ErrorMessage = "Please select category")]
        public string Category { get; set; } = null!;

        [Required(ErrorMessage = "Please select status")]
        public string Status { get; set; } = null!;

        //[Required(ErrorMessage = "Please select date")]
        [DataType(DataType.Date)]
        public DateTime DateTime { get; set; }

        [Required(ErrorMessage = "Please select priority")]
        public string Priority { get; set; } = null!;

        //[Required(ErrorMessage = "Please select employee")]
        //Generated by system?
        public int Employee { get; set; }

        public string? DevicesInvolved { get; set; } = null;

        public string? Additional_Details { get; set; } = null;

        public string Resolution { get; set; } = null!;
    }
}
