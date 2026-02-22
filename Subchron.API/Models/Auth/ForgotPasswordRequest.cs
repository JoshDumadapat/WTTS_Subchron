namespace Subchron.API.Models.Auth
{
    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = null!;
        public string RecaptchaToken { get; set; } = "";
    }
}
