namespace api.Dtos
{
    public class ResetPasswordRequest
    {
        public string Email { get; set; }
        public int Otp { get; set; }
        public string NewPassword { get; set; }
    }
}
