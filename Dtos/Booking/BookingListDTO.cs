using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Dtos.Booking
{
    public class BookingListDTO
    {
        public int ID { get; set; }
        public string UserEmail { get; set; }
        public int TimeSlotID { get; set; }
        public bool IsConfirmed { get; set; }
        // Add other simple fields if needed, but NOT PaymentScreenshot or navigation properties
    }
}