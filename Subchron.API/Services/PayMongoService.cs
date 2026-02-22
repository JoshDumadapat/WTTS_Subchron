using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Subchron.API.Models.Settings;

namespace Subchron.API.Services;

// PayMongo v1 client for creating payment intents and checking payment status; use test keys in dev, live in prod.
public class PayMongoService
{
    private const string BaseUrl = "https://api.paymongo.com/v1";
    private readonly HttpClient _http;
    private readonly PayMongoSettings _settings;

    public PayMongoService(HttpClient http, IOptions<PayMongoSettings> settings)
    {
        _http = http;
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        var secretKey = _settings.SecretKey ?? "";
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException("PayMongo:SecretKey is not set in configuration.");
        _http.BaseAddress = new Uri(BaseUrl);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes(secretKey.Trim() + ":")));
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    // Creates a payment intent (amount in pesos, converted to centavos) and returns the client key for the frontend.
    public async Task<PayMongoPaymentIntentResult?> CreatePaymentIntentAsync(
        decimal amountPesos,
        string currency = "PHP",
        string? description = null,
        string[]? paymentMethodAllowed = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var amountCentavos = (int)Math.Round(amountPesos * 100m);
        if (amountCentavos < 2000) amountCentavos = 2000; // PayMongo minimum

        // PayMongo rejects empty metadata; use at least one key-value pair
        var meta = metadata != null && metadata.Count > 0
            ? metadata
            : new Dictionary<string, string> { ["source"] = "subchron_signup" };

        var payload = new
        {
            data = new
            {
                attributes = new
                {
                    amount = amountCentavos,
                    currency,
                    description = description ?? "Subchron subscription",
                    payment_method_allowed = paymentMethodAllowed ?? new[] { "card", "gcash", "paymaya" },
                    metadata = meta
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync("payment_intents", content, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errMsg = TryGetPayMongoErrorMessage(responseJson) ?? $"HTTP {(int)response.StatusCode}";
            throw new InvalidOperationException($"PayMongo: {errMsg}");
        }

        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var data = doc.RootElement.GetProperty("data");
            var id = data.GetProperty("id").GetString();
            var attrs = data.GetProperty("attributes");
            var clientKey = attrs.TryGetProperty("client_key", out var ck) ? ck.GetString() : null;
            var status = attrs.TryGetProperty("status", out var st) ? st.GetString() : null;

            return new PayMongoPaymentIntentResult
            {
                Id = id!,
                ClientKey = clientKey ?? "",
                Status = status ?? "awaiting_payment_method",
                Amount = amountCentavos
            };
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"PayMongo: Invalid response - {ex.Message}", ex);
        }
    }

    private static string? TryGetPayMongoErrorMessage(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Array && errs.GetArrayLength() > 0)
            {
                var first = errs[0];
                if (first.TryGetProperty("detail", out var detail)) return detail.GetString();
                if (first.TryGetProperty("title", out var title)) return title.GetString();
            }
        }
        catch { /* ignore */ }
        return null;
    }

    // Fetches a payment intent with status, payment id (if any), and last_payment_error for audit.
    public async Task<PayMongoPaymentIntentResult?> GetPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync($"payment_intents/{paymentIntentId}", cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var data = doc.RootElement.GetProperty("data");
            var id = data.GetProperty("id").GetString();
            var attrs = data.GetProperty("attributes");
            var status = attrs.TryGetProperty("status", out var st) ? st.GetString() : null;
            var amount = attrs.TryGetProperty("amount", out var am) ? am.GetInt32() : 0;

            string? paymentId = null;
            if (data.TryGetProperty("relationships", out var rel) && rel.TryGetProperty("payments", out var pay) && pay.TryGetProperty("data", out var payData) && payData.ValueKind == JsonValueKind.Array && payData.GetArrayLength() > 0)
                paymentId = payData[0].GetProperty("id").GetString();

            string? errorCode = null, errorMessage = null;
            if (attrs.TryGetProperty("last_payment_error", out var err) && err.ValueKind != JsonValueKind.Null)
            {
                if (err.TryGetProperty("code", out var ec)) errorCode = ec.GetString();
                if (err.TryGetProperty("message", out var em)) errorMessage = em.GetString();
            }

            return new PayMongoPaymentIntentResult
            {
                Id = id!,
                ClientKey = "",
                Status = status ?? "",
                Amount = amount,
                PayMongoPaymentId = paymentId,
                LastPaymentErrorCode = errorCode,
                LastPaymentErrorMessage = errorMessage
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Verify webhook signature using PayMongo-Signature header and WebhookSecret. Returns true if valid or if WebhookSecret is not set.</summary>
    public bool VerifyWebhookSignature(string payload, string signatureHeader)
    {
        var secret = _settings.WebhookSecret;
        if (string.IsNullOrWhiteSpace(secret)) return true;
        if (string.IsNullOrEmpty(signatureHeader)) return false;
        // PayMongo uses HMAC SHA256: hex(signature) = hmac_sha256(webhook_secret, payload)
        using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret.Trim()));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computed = Convert.ToHexString(hash).ToLowerInvariant();
        return string.Equals(signatureHeader.Trim(), computed, StringComparison.OrdinalIgnoreCase);
    }
}

public class PayMongoPaymentIntentResult
{
    public string Id { get; set; } = null!;
    public string ClientKey { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int Amount { get; set; }
    public string? PayMongoPaymentId { get; set; }
    public string? LastPaymentErrorCode { get; set; }
    public string? LastPaymentErrorMessage { get; set; }
}
