using System.ComponentModel.DataAnnotations;

namespace api.Models.DTOs
{
    public class TimeSlotCreateUpdateDTO
    {
        [Required(ErrorMessage = "Start time is required.")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "End time is required.")]
        public TimeSpan EndTime { get; set; }
    }
}