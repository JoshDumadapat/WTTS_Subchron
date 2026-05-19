namespace Subchron.Web.Infrastructure;

public static class SubchronApiHttpClientExtensions
{
    public const string ClientName = "Subchron.API";

    public static IServiceCollection AddSubchronApiHttpClient(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddHttpClient(ClientName, client =>
            {
                // Prefer ApiInternalUrl for server-to-server calls when set.
                var api = (configuration["ApiInternalUrl"] ?? configuration["ApiBaseUrl"] ?? "").Trim().TrimEnd('/');
                if (environment.IsDevelopment() && api.Contains("localhost", StringComparison.OrdinalIgnoreCase))
                    api = api.Replace("localhost", "127.0.0.1", StringComparison.OrdinalIgnoreCase);

                if (!string.IsNullOrWhiteSpace(api))
                    client.BaseAddress = new Uri(api + "/");
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler
                {
                    // System proxy (Fiddler, etc.) often breaks localhost API calls.
                    UseProxy = false
                };

                if (environment.IsDevelopment())
                {
                    handler.ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }

                return handler;
            });

        return services;
    }
}
