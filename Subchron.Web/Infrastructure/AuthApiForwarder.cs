using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace Subchron.Web.Infrastructure;

/// <summary>
/// Forwards auth API calls from Razor Pages to Subchron.API with the caller's IP.
/// </summary>
public class AuthApiForwarder
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<AuthApiForwarder> _logger;

    public AuthApiForwarder(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<AuthApiForwarder> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public Task<IActionResult> ForwardGetAsync(HttpContext httpContext, string relativePath) =>
        ForwardAsync(httpContext, HttpMethod.Get, relativePath);

    public Task<IActionResult> ForwardPostAsync(HttpContext httpContext, string relativePath) =>
        ForwardAsync(httpContext, HttpMethod.Post, relativePath);

    private async Task<IActionResult> ForwardAsync(
        HttpContext httpContext,
        HttpMethod method,
        string relativePath)
    {
        var client = _httpClientFactory.CreateClient(SubchronApiHttpClientExtensions.ClientName);
        var path = relativePath.TrimStart('/');
        var absoluteUri = BuildAbsoluteUri(path, httpContext.Request.QueryString.Value);

        using var request = new HttpRequestMessage(method, absoluteUri);

        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrEmpty(clientIp))
            request.Headers.TryAddWithoutValidation("X-Forwarded-For", clientIp);

        if (HttpMethods.IsPost(method.Method) ||
            HttpMethods.IsPut(method.Method) ||
            HttpMethods.IsPatch(method.Method))
        {
            var body = await ReadRequestBodyAsync(httpContext);
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                httpContext.RequestAborted);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or IOException)
        {
            var apiUrl = GetConfiguredApiUrl();
            _logger.LogWarning(ex, "Auth API call failed: {Method} {Uri}", method, absoluteUri);

            var message = string.IsNullOrEmpty(apiUrl)
                ? "API URL is not configured."
                : $"Could not reach the API at {apiUrl}. Start Subchron.API, then try again.";

            if (_environment.IsDevelopment())
                message += $" ({ex.GetType().Name}: {ex.Message})";

            return new JsonResult(new { ok = false, message })
            { StatusCode = StatusCodes.Status503ServiceUnavailable };
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(httpContext.RequestAborted);
            return new ContentResult
            {
                Content = body,
                ContentType = response.Content.Headers.ContentType?.MediaType ?? "application/json",
                StatusCode = (int)response.StatusCode
            };
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpContext httpContext)
    {
        httpContext.Request.EnableBuffering();
        httpContext.Request.Body.Position = 0;

        using var reader = new StreamReader(
            httpContext.Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync(httpContext.RequestAborted);
        httpContext.Request.Body.Position = 0;
        return body;
    }

    private Uri BuildAbsoluteUri(string path, string? query)
    {
        var baseUrl = GetConfiguredApiUrl().TrimEnd('/');
        var uriText = string.IsNullOrEmpty(query)
            ? $"{baseUrl}/{path}"
            : $"{baseUrl}/{path}{query}";
        return new Uri(uriText);
    }

    private string GetConfiguredApiUrl() =>
        (_configuration["ApiInternalUrl"] ?? _configuration["ApiBaseUrl"] ?? "").Trim();
}
