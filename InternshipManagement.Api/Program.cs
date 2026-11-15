using InternshipManagement.Api.Extensions;
using InternshipManagement.Api.Middleware;
using InternshipManagement.Infrastructure.Integrations.N8n;
using InternshipManagement.Infrastructure.Identity;
using InternshipManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddSmtpEmail(builder.Configuration);
builder.Services.AddHostedService<InternshipManagement.Api.Jobs.EmailVerificationHousekeepingService>();
builder.Services.AddHostedService<InternshipManagement.Api.Jobs.InternshipDeadlineService>();
builder.Services.AddValidation();
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options => options.AddPolicy("Web", policy =>
{
    if (allowedOrigins.Length > 0)
        policy.WithOrigins(allowedOrigins)
              .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
              .WithHeaders("Authorization", "Content-Type", "Accept");
}));

builder.Services.AddOpenApiDocumentation();  // ✅ Using new OpenAPI

// n8n automation (AI workflows run in n8n; the API stays in control of rules and data)
builder.Services.AddN8nIntegration(builder.Configuration);

// Each AI call costs Gemini quota: limit each signed-in user to 10 per hour
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("ai-generation", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindFirst("userId")?.Value
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromHours(1),
                QueueLimit = 0
            }));
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue("LoginSecurity:LoginAttemptsPerIpPerMinute", 20),
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.AddPolicy("password-reset", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(15),
                QueueLimit = 0
            }));
});

// A limit on every request, a clear 429 answer, and the real visitor address from the Web project or a proxy.
// Must come after AddRateLimiter.
builder.Services.AddPlatformProtection(builder.Configuration);

// Add controllers
builder.Services.AddControllers(o => o.Filters.Add<InternshipManagement.Api.Filters.AuditActionFilter>());

var app = builder.Build();

// Must be first, so everything after it sees the real visitor address.
app.UseForwardedHeaders();

// ✅ NEW: .NET 10 OpenAPI with Scalar UI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();  // ✅ Generates the OpenAPI document

    // ✅ Use Scalar UI instead of Swagger UI
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Internship Management API v1");
    });
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseCors("Web");
app.UseAuthentication();
app.UseMiddleware<SessionSecurityMiddleware>();
app.UseMiddleware<UnverifiedAccountMiddleware>();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

// Seed roles and admin user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
        await SeedDatabaseAsync(services, builder.Configuration);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();

async Task SeedDatabaseAsync(IServiceProvider services, IConfiguration configuration)
{
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    string[] roles = { "Student", "Company", "Admin" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<int>(role));
        }
    }

    // The first administrator. The password is never kept in a file: set it with
    //   dotnet user-secrets set "Seed:AdminPassword" "<a strong password>" --project InternshipManagement.Api
    // (on a server, use the environment variable Seed__AdminPassword). It is only used when the account does not exist yet.
    var adminEmail = configuration["Seed:AdminEmail"] ?? "admin@internshipmanagement.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        var adminPassword = configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning("No administrator exists and Seed:AdminPassword is not set, so none was created.");
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FirstName = "Admin",
            LastName = "User",
            UserType = "Admin",
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }
        else
        {
            logger.LogError("The administrator could not be created: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}
