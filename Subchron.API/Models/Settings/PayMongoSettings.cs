namespace Subchron.API.Models.Settings;

// PayMongo API keys; use test keys (sk_test_..., pk_test_...) when testing.
public class PayMongoSettings
{
    public string SecretKey { get; set; } = null!;
    public string PublicKey { get; set; } = null!;
    // Optional webhook signing secret from PayMongo dashboard to verify webhook requests.
    public string? WebhookSecret { get; set; }
}
