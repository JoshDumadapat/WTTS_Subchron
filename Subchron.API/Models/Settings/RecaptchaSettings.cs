namespace Subchron.API.Models.Settings
{
    public class RecaptchaSettings
    {
        public string SecretKey { get; set; } = null!;
        public double MinimumScore { get; set; } = 0.5;
    }
}
