﻿using System.ComponentModel.DataAnnotations;

namespace FYP.Models
{
    public class Ticket
    {
        public int TicketId { get; set; }

        [RegularExpression("(\\d{8}|\\d{4})", ErrorMessage = "either 4 or 8 digits only")]
        public int UserId { get; set; }

        public string Email { get; set; } = null!;

        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "Please select ticket type")]
        public string Type { get; set; } = null!;

        [Required(ErrorMessage = "Please enter description")]
        [StringLength(250, ErrorMessage = "Maximum is 250 characters")]
        public string Description { get; set; } = null!;

        [Required(ErrorMessage = "Please select category")]
        public string Category { get; set; } = null!;

        [Required(ErrorMessage = "Please select status")]
        public string Status { get; set; } = null!;

        public string newStatus { get; set; } = null!;

        //[Required(ErrorMessage = "Please select date")]
        [DataType(DataType.Date)]
        public DateTime DateTime { get; set; }

        [Required(ErrorMessage = "Please select priority")]
        public string Priority { get; set; } = null!;

        //[Required(ErrorMessage = "Please select employee")]
        //Generated by system?
        public int Employee { get; set; }

        public string EmployeeName { get; set; } = null!;

        public string? DevicesInvolved { get; set; } = null;

        public string? Additional_Details { get; set; } = null;

        public string? Resolution { get; set; } = null;

        public string? Escalate_Reason { get; set; } = null;

        public int Escalate_SE { get; set; }

        //For Data Collection
        public int StatusCount { get; set; }

        public int PriorityCount { get; set; }

        public int CategoryCount { get; set; }

        public int TypeCount { get; set; }
        public int category_id { get; set; }
    }
}