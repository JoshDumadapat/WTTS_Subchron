namespace Subchron.API.Models.Auth
{
    public class LoginRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string RecaptchaToken { get; set; } = "";
    }
}
