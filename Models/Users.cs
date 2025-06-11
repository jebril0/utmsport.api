using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models
{
    public class Users
    {
        public int ID { get; set; }

        [Required]
        [RegularExpression(@"^[^@\s]+@graduate\.utm\.my$", ErrorMessage = "Email must be a valid @graduate.utm.my address.")]
        public string Email { get; set; } = string.Empty; // Email will be configured as unique in the DbContext

        [StringLength(18, ErrorMessage = "Username must be between 6 and 18 characters long.", MinimumLength = 6)]
        public string Name { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string Rolebase { get; set; } = string.Empty;

        public ICollection<TimeSlot>? BookedTimeSlots { get; set; }

        public int FailedLoginAttempts { get; set; } = 0;

        public DateTime? LockoutEnd { get; set; }

        public bool IsLoginLockoutEnabled { get; set; } = true; // To enable/disable this feature

        public int? RegistrationOtp { get; set; } = null;
        public DateTime? RegistrationOtpExpiry { get; set; } = null;
        public bool IsEmailVerified { get; set; } = false;

        // Added fields for password reset
        public int? PasswordResetOtp { get; set; }
        public DateTime? PasswordResetOtpExpiry { get; set; }
    }
}