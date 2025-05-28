using System.ComponentModel.DataAnnotations;

namespace api.Models.DTOs
{
    public class VenuesDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public string Type { get; set; } = string.Empty;
        public bool Status { get; set; }
    }
}