using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Subchron.API.Data;
using Subchron.API.Models.Settings;
using Subchron.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// JWT Bearer for authenticated API calls (e.g. change-password, subscription/usage)
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();
if (jwt != null && !string.IsNullOrEmpty(jwt.Secret))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });
    builder.Services.AddAuthorization();
}

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// EF Core: Platform DB (DefaultConnection) and Tenant DB (TenantConnection) — two physical databases
var defaultConnection =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var tenantConnection =
    builder.Configuration.GetConnectionString("TenantConnection")
    ?? throw new InvalidOperationException("Connection string 'TenantConnection' not found.");

builder.Services.AddDbContext<SubchronDbContext>(options =>
    options.UseSqlServer(defaultConnection, sql =>
    {
        sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    }));

builder.Services.AddDbContext<TenantDbContext>(options =>
    options.UseSqlServer(tenantConnection, sql =>
    {
        sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    }));

// Settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<RecaptchaSettings>(builder.Configuration.GetSection("Recaptcha"));
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<PayMongoSettings>(builder.Configuration.GetSection("PayMongo"));
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("Cloudinary"));
builder.Services.Configure<LocationIqSettings>(builder.Configuration.GetSection("LocationIq"));

// Services
builder.Services.AddHttpClient<RecaptchaService>();
builder.Services.AddHttpClient<PayMongoService>();
builder.Services.AddHttpClient<ILocationIqService, LocationIqService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddHostedService<PendingPaymentCleanupService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

// CORS (Web -> API)
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebCors", p =>
    {
        p.WithOrigins(allowedOrigins)
         .AllowAnyHeader()
         .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("StartupMigration");

    var autoMigrate = app.Configuration.GetValue<bool>("Database:AutoMigrate", true);

    if (autoMigrate)
    {
        try
        {
            var db = services.GetRequiredService<SubchronDbContext>();

            // Prevent infinite startup hang when DB host is down
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(25));

            // Quick reachability test
            if (!await db.Database.CanConnectAsync(cts.Token))
            {
                logger.LogWarning("Database not reachable. Skipping migrations so the app can start.");
            }
            else
            {
                // Retry migrations a few times for transient network hiccups
                const int maxAttempts = 5;

                for (var attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        logger.LogInformation("Applying Platform (SubchronDbContext) migrations... attempt {Attempt}/{Max}", attempt, maxAttempts);
                        await db.Database.MigrateAsync(cts.Token);
                        logger.LogInformation("Platform migrations applied successfully.");
                        break;
                    }
                    catch (Exception ex) when (attempt < maxAttempts)
                    {
                        logger.LogWarning(ex, "Migration attempt {Attempt} failed. Retrying...", attempt);
                        await Task.Delay(TimeSpan.FromSeconds(2), cts.Token);
                    }
                }
            }

            // Tenant DB migrations (separate database)
            try
            {
                var tenantDb = services.GetRequiredService<TenantDbContext>();
                if (await tenantDb.Database.CanConnectAsync(cts.Token))
                {
                    await tenantDb.Database.MigrateAsync(cts.Token);
                    logger.LogInformation("Tenant DB migrations applied successfully.");
                }
                else
                    logger.LogWarning("Tenant DB not reachable. Skipping tenant migrations.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Tenant DB migration failed during startup.");
            }
        }
        catch (Exception ex)
        {
            // Important: do NOT throw (prevents 500.30 / app crash)
            logger.LogError(ex, "Database migration failed during startup.");
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("WebCors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();