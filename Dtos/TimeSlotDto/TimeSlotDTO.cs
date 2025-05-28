namespace api.Models.DTOs
{
    public class TimeSlotDTO
    {
        public int ID { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsAvailable { get; set; }
       
    }
}