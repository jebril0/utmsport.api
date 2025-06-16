using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace api.Models
{
    public class Venues
    {
        [Key]
        public string Name { get; set; } = string.Empty; // Primary Key

        [Required(ErrorMessage = "Venue location is required.")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Venue capacity is required.")]
        public int Capacity { get; set; }

        [Required(ErrorMessage = "Venue type is required.")]
        public string Type { get; set; } = string.Empty;

        [Required(ErrorMessage = "Venue status is required.")]
        public bool Status { get; set; } // True for active, False for inactive

        [Required(ErrorMessage = "Venue price is required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive value.")]
        public decimal Price { get; set; } // Price per booking

        public ICollection<TimeSlot>? TimeSlots { get; set; } // Navigation property
    }
}