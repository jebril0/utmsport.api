namespace api.Dtos
{
    public class VerifyOtpRequest
    {
        public required string Email { get; set; }
        public int Otp { get; set; }
    }
}
