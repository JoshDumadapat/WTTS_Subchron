using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Razor Pages routing + authorization conventions
builder.Services.AddRazorPages(options =>
{
    // PUBLIC
    options.Conventions.AllowAnonymousToFolder("/Landing");
    options.Conventions.AllowAnonymousToFolder("/Auth");
    options.Conventions.AllowAnonymousToPage("/Index");

    // PRIVATE AREAS
    options.Conventions.AuthorizeFolder("/SuperAdmin");
    options.Conventions.AuthorizeFolder("/App");
    options.Conventions.AuthorizeFolder("/Employee");
});

// Cookie auth
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; // app cookie
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";

        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);

        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        // In development (HTTP) allow cookie so "Back to Admin" from Employee portal works without redirect to login
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    })
    .AddCookie("External", options =>
    {
        options.Cookie.Name = "Subchron.External";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    })
    .AddGoogle("Google", options =>
    {
        options.ClientId = builder.Configuration["GoogleAuth:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["GoogleAuth:ClientSecret"] ?? "";
        options.SaveTokens = true;

        // Critical: Google must NOT sign into the main app cookie
        options.SignInScheme = "External";
    });

// Authorization policies (use these on pages/modules)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdminOnly", p => p.RequireRole("SuperAdmin"));

    // Backoffice roles (RBAC: see Subchron.Web/Rbac and Subchron.API/Authorization)
    options.AddPolicy("Backoffice", p => p.RequireRole("OrgAdmin", "HR", "Manager", "Supervisor", "Payroll"));
    options.AddPolicy("OrgAdminOnly", p => p.RequireRole("OrgAdmin"));
    options.AddPolicy("HROnly", p => p.RequireRole("HR"));
    options.AddPolicy("ManagerOnly", p => p.RequireRole("Manager"));
    options.AddPolicy("SupervisorOnly", p => p.RequireRole("Supervisor"));
    options.AddPolicy("PayrollOnly", p => p.RequireRole("Payroll"));
    options.AddPolicy("Approvers", p => p.RequireRole("OrgAdmin", "HR", "Manager", "Supervisor"));

    // Employee portal
    options.AddPolicy("EmployeeOnly", p => p.RequireRole("Employee"));
});

// Named HttpClient to call API
builder.Services.AddHttpClient("Subchron.API", client =>
{
    var api = (builder.Configuration["ApiBaseUrl"] ?? "").TrimEnd('/');
    if (!string.IsNullOrWhiteSpace(api))
        client.BaseAddress = new Uri(api + "/");
});

var app = builder.Build();

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// In Development: show friendly message when API (localhost:7077) is not running
if (app.Environment.IsDevelopment())
{
    app.Use(async (ctx, next) =>
    {
        try
        {
            await next();
        }
        catch (Exception ex)
        {
            if (ctx.Response.HasStarted) throw;
            var isRefused = ex.Message.Contains("refused", StringComparison.OrdinalIgnoreCase) ||
                            ex.Message.Contains("7077", StringComparison.OrdinalIgnoreCase);
            var isConnectionError = ex is HttpRequestException || ex is System.Net.Sockets.SocketException;
            if (isConnectionError && isRefused)
            {
                ctx.Response.StatusCode = 503;
                ctx.Response.ContentType = "text/html; charset=utf-8";
                var apiUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7077";
                await ctx.Response.WriteAsync($$"""
                    <!DOCTYPE html>
                    <html>
                    <head>
                      <meta charset="utf-8"/>
                      <meta name="viewport" content="width=device-width, initial-scale=1"/>
                      <title>API not running - Subchron</title>
                      <style>
                        body{ font-family: system-ui,sans-serif; max-width: 36em; margin: 2em auto; padding: 0 1em; }
                        code{ background:#f1f5f9; padding: .2em .4em; }
                        a{ color: #16a34a; }
                      </style>
                    </head>
                    <body>
                      <h1>Subchron API is not running</h1>
                      <p>The web app could not connect to the API at <code>{{apiUrl}}</code> (connection refused).</p>
                      <p><strong>Fix:</strong> Start the <strong>Subchron.API</strong> project, then refresh this page.</p>
                      <ul>
                        <li>In Visual Studio: set both Subchron.Web and Subchron.API as startup projects, or run Subchron.API first.</li>
                        <li>From terminal: <code>dotnet run --project Subchron.API</code> (then run the Web app).</li>
                      </ul>
                      <p><a href="{{ctx.Request.Path}}">Refresh</a></p>
                    </body>
                    </html>
                    """);
                return;
            }
            throw;
        }
    });
}

// Apply no-cache headers to ALL dynamic pages (not static files)
// This prevents browser back button from showing cached protected pages after logout
app.Use(async (ctx, next) =>
{
    // Skip static files
    var path = ctx.Request.Path.Value ?? "";
    if (path.StartsWith("/css") || path.StartsWith("/js") || path.StartsWith("/images") ||
        path.StartsWith("/lib") || path.Contains("."))
    {
        await next();
        return;
    }

    // Set no-cache headers before the response starts
    ctx.Response.OnStarting(() =>
    {
        if (!ctx.Response.Headers.ContainsKey("Cache-Control"))
        {
            ctx.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
            ctx.Response.Headers["Pragma"] = "no-cache";
            ctx.Response.Headers["Expires"] = "0";
        }
        return Task.CompletedTask;
    });

    await next();
});

// Redirect logged-in users away from public pages (Landing/Auth/Index)
app.Use(async (ctx, next) =>
{
    // Only redirect on GET navigations, never on POST/API calls
    if (!HttpMethods.IsGet(ctx.Request.Method))
    {
        await next();
        return;
    }

    var path = (ctx.Request.Path.Value ?? "").ToLowerInvariant();
    var isAuth = ctx.User?.Identity?.IsAuthenticated == true;

    if (path.Contains("/auth/logout"))
    {
        await next();
        return;
    }

    // Allow /Auth/Billing when token is present (trial-expired → Subscribe flow)
    if (path.StartsWith("/auth/billing") && !string.IsNullOrWhiteSpace(ctx.Request.Query["token"]))
    {
        await next();
        return;
    }

    bool isPublicArea =
        path == "/" ||
        path.StartsWith("/index") ||
        path.StartsWith("/landing") ||
        path.StartsWith("/auth");

    if (isAuth && isPublicArea && ctx.User != null)
    {
        var role = (ctx.User.FindFirst(ClaimTypes.Role)?.Value
            ?? ctx.User.FindFirst("role")?.Value
            ?? "").Trim();

        var dest = string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ? "/SuperAdmin/Dashboard"
            : string.Equals(role, "Employee", StringComparison.OrdinalIgnoreCase) ? "/Employee/Dashboard"
            : string.Equals(role, "OrgAdmin", StringComparison.OrdinalIgnoreCase) ? "/App/Dashboard"
            : string.Equals(role, "HR", StringComparison.OrdinalIgnoreCase) ? "/App/Dashboard"
            : string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase) ? "/App/Dashboard"
            : string.Equals(role, "Supervisor", StringComparison.OrdinalIgnoreCase) ? "/App/Dashboard"
            : string.Equals(role, "Payroll", StringComparison.OrdinalIgnoreCase) ? "/App/Dashboard"
            : "/App/Dashboard";

        if (!path.StartsWith(dest.ToLowerInvariant()))
        {
            ctx.Response.Redirect(dest);
            return;
        }
    }

    await next();
});

app.MapRazorPages();

app.Run();