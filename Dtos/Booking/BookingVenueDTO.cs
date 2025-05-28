using System.ComponentModel.DataAnnotations;

namespace api.Models.DTOs
{
    public class BookingVenueDTO
    {
        [Required(ErrorMessage = "User email is required.")]
        public string UserEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Venue name is required.")]
        public string VenueName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start time is required.")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "End time is required.")]
        public TimeSpan EndTime { get; set; }

        [Required(ErrorMessage = "Payment screenshot is required.")]
        public required IFormFile PaymentScreenshot { get; set; }
    }
}