using System.ComponentModel.DataAnnotations;

namespace api.Models.DTOs
{
    public class VenuesCreateUpdateDTO
    {
        [Required(ErrorMessage = "Venue name is required.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Venue location is required.")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Venue capacity is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Capacity must be greater than 0.")]
        public int Capacity { get; set; }

        [Required(ErrorMessage = "Venue type is required.")]
        public string Type { get; set; } = string.Empty;

        [Required(ErrorMessage = "Venue status is required.")]
        public bool Status { get; set; }
  
        public decimal Price { get; set; } // <-- Add this line
    }
}