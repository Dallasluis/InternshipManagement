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
builder.Services.AddCorsPolicy();
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
});

// Add controllers
builder.Services.AddControllers();

var app = builder.Build();

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
app.UseCors("AllowAll");
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
        await SeedDatabaseAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();

async Task SeedDatabaseAsync(IServiceProvider services)
{
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();

    string[] roles = { "Student", "Company", "Admin" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<int>(role));
        }
    }

    var adminEmail = "admin@internshipmanagement.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
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

        var result = await userManager.CreateAsync(admin, "Admin@123456");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}