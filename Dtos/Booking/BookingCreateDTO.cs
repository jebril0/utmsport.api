using System.ComponentModel.DataAnnotations;

namespace api.Models.DTOs
{
    public class BookingCreateDTO
    {
        [Required(ErrorMessage = "User email is required.")]
        public string UserEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Time slot ID is required.")]
        public int TimeSlotID { get; set; }

        [Required(ErrorMessage = "Payment screenshot is required.")]
        public required byte[] PaymentScreenshot { get; set; }
    }
}