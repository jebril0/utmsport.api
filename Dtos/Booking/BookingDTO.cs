namespace api.Models.DTOs
{
    public class BookingDTO
    {
        public int ID { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public int TimeSlotID { get; set; }
        public bool IsConfirmed { get; set; }
    }
}