using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class Booking
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        public string UserEmail { get; set; } = string.Empty;

        [Required]
        public int TimeSlotID { get; set; } // Foreign Key to TimeSlot

        public byte[]? PaymentScreenshot { get; set; } // Stores the payment screenshot as binary data

        public bool IsConfirmed { get; set; } = false; // Default to not confirmed
        public string QrToken { get; set; } = string.Empty; // QR code token for the booking
        // Navigation properties
      public required Users User { get; set; }

        public TimeSlot? TimeSlot { get; set; }
    }
}