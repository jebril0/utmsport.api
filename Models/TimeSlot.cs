using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class TimeSlot
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // Auto-increment ID
        public int ID { get; set; } // Primary Key

        [Required(ErrorMessage = "Venue name is required.")]
        [ForeignKey("Venue")]
        public string VenueName { get; set; } = string.Empty; // Foreign Key to Venue table

        [Required(ErrorMessage = "Start time is required.")]
        public TimeSpan StartTime { get; set; } // e.g., 08:00

        [Required(ErrorMessage = "End time is required.")]
        public TimeSpan EndTime { get; set; } // e.g., 10:00

        [Required(ErrorMessage = "Availability status is required.")]
        public bool IsAvailable { get; set; } = true; // Default value set to true

        // Navigation property for the venue
        public Venues? Venue { get; set; }
    }
}