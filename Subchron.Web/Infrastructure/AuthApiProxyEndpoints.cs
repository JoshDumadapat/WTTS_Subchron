using System.Net.Http.Headers;

namespace Subchron.Web.Infrastructure;

/// <summary>
/// Forwards anonymous auth API calls from the browser to Subchron.API via server-side HttpClient
/// (avoids cross-origin calls and HTTPS port mismatches on the login page).
/// </summary>
public static class AuthApiProxyEndpoints
{
    public const string Prefix = "/auth-api";

    public static IEndpointRouteBuilder MapAuthApiProxy(this IEndpointRouteBuilder endpoints)
    {
        endpoints.Map($"{Prefix}/{{**path}}", ProxyAsync);
        return endpoints;
    }

    private static async Task ProxyAsync(
        HttpContext context,
        string? path,
        IHttpClientFactory httpClientFactory,
        IConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var apiBase = (config["ApiBaseUrl"] ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(apiBase))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                ok = false,
                message = "ApiBaseUrl is not configured. Set it to https://localhost:7077 in appsettings and start Subchron.API."
            });
            return;
        }

        var client = httpClientFactory.CreateClient(SubchronApiHttpClientExtensions.ClientName);
        var target = $"api/auth/{path}{context.Request.QueryString}";

        using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), target);

        if (HttpMethods.IsPost(context.Request.Method) ||
            HttpMethods.IsPut(context.Request.Method) ||
            HttpMethods.IsPatch(context.Request.Method))
        {
            request.Content = new StreamContent(context.Request.Body);
            if (!string.IsNullOrWhiteSpace(context.Request.ContentType))
            {
                request.Content.Headers.ContentType =
                    MediaTypeHeaderValue.Parse(context.Request.ContentType);
            }
        }

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                context.RequestAborted);
        }
        catch (HttpRequestException ex)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "application/json";
            var hint = ex.InnerException?.Message?.Contains("certificate", StringComparison.OrdinalIgnoreCase) == true
                ? " If the API uses HTTPS, ensure the ASP.NET dev certificate is trusted (dotnet dev-certs https --trust)."
                : "";
            await context.Response.WriteAsJsonAsync(new
            {
                ok = false,
                message = $"Could not reach the API at {apiBase}. Start Subchron.API with the HTTPS profile, then try again.{hint}"
            });
            return;
        }

        using (response)
        {
            context.Response.StatusCode = (int)response.StatusCode;
            if (response.Content.Headers.ContentType is { } contentType)
                context.Response.ContentType = contentType.ToString();

            await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
        }
    }
}
