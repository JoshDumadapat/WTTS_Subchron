using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Subchron.API.Models.Settings;

namespace Subchron.API.Services;

public class RecaptchaService
{
    private readonly HttpClient _http;
    private readonly RecaptchaSettings _settings;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public RecaptchaService(HttpClient http, IOptions<RecaptchaSettings> settings)
    {
        _http = http;
        _settings = settings.Value;
    }

    // Verifies the reCAPTCHA v2 token from the client; returns true when it’s valid.
    public async Task<bool> VerifyAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(_settings.SecretKey))
            throw new InvalidOperationException("reCAPTCHA SecretKey is not configured.");

        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            using var cts = new CancellationTokenSource(Timeout);

            var form = new Dictionary<string, string>
            {
                ["secret"] = _settings.SecretKey,
                ["response"] = token
            };

            using var resp = await _http.PostAsync(
                "https://www.google.com/recaptcha/api/siteverify",
                new FormUrlEncodedContent(form),
                cts.Token);

            if (!resp.IsSuccessStatusCode)
                return false;

            var json = await resp.Content.ReadFromJsonAsync<RecaptchaV2Response>(cancellationToken: cts.Token);

            // v2 checkbox only needs success = true (no score)
            return json?.success == true;
        }
        catch (OperationCanceledException)
        {
            // Timeout - fail fast
            return false;
        }
        catch (HttpRequestException)
        {
            // Network error - fail fast
            return false;
        }
    }

    private sealed class RecaptchaV2Response
    {
        public bool success { get; set; }
        public string? challenge_ts { get; set; }
        public string? hostname { get; set; }
        public string[]? error_codes { get; set; }
    }
}
