using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Net.Mail;
using InternshipManagement.Api.Jobs;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Application.Services;
using InternshipManagement.Application.Validators;
using InternshipManagement.Infrastructure.Documents;
using InternshipManagement.Infrastructure.Email;
using InternshipManagement.Infrastructure.Identity;
using InternshipManagement.Infrastructure.Persistence;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.OpenApi;  // ✅ Use this instead of Microsoft.OpenApi
using Microsoft.OpenApi;      // ✅ Still needed for OpenApi types


namespace InternshipManagement.Api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            return services;
        }

        public static IServiceCollection AddIdentityServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var securitySettings = configuration.GetSection("LoginSecurity");
            services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = securitySettings.GetValue("PasswordMinimumLength", 10);
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(
                    securitySettings.GetValue("InitialLockoutMinutes", 15));
                options.Lockout.MaxFailedAccessAttempts = securitySettings.GetValue("LockoutMaxFailedAttempts", 5);
                options.Lockout.AllowedForNewUsers = true;
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            services.Configure<PasswordHasherOptions>(options =>
                options.IterationCount = securitySettings.GetValue("PasswordHashIterations", 600_000));

            services.AddOptions<LoginSecurityOptions>()
                .Bind(securitySettings)
                .Validate(options => options.PasswordMinimumLength >= 10,
                    "LoginSecurity:PasswordMinimumLength must be at least 10.")
                .Validate(options => options.PasswordHashIterations >= 600_000,
                    "LoginSecurity:PasswordHashIterations must be at least 600000.")
                .Validate(options => options.LockoutMaxFailedAttempts > 0
                    && options.LockoutWindowMinutes > 0
                    && options.InitialLockoutMinutes > 0
                    && options.MaxLockoutMinutes >= options.InitialLockoutMinutes,
                    "LoginSecurity lockout values are invalid.")
                .Validate(options => options.LoginAttemptsPerIpPerMinute > 0,
                    "LoginSecurity:LoginAttemptsPerIpPerMinute must be positive.")
                .Validate(options => options.StudentIdleTimeoutHours > 0
                    && options.AdminCompanyIdleTimeoutMinutes > 0
                    && options.AbsoluteSessionHours > 0
                    && options.RememberStudentDays > 0
                    && options.SecurityEventRetentionMonths > 0,
                    "LoginSecurity session and retention values must be positive.")
                .Validate(options => options.OtpLifetimeMinutes > 0
                    && options.OtpMaxAttempts > 0
                    && options.OtpResendSeconds >= 0,
                    "LoginSecurity one-time code values are invalid.")
                .ValidateOnStart();

            services.AddOptions<EmailVerificationOptions>()
                .Bind(configuration.GetSection("EmailVerification"))
                .Validate(options => options.LinkExpiryHours > 0,
                    "EmailVerification:LinkExpiryHours must be positive.")
                .Validate(options => options.ResendsPerHour > 0 && options.ResendsPerDay > 0,
                    "Email verification resend limits must be positive.")
                .Validate(options => options.ReminderAfterDays > 0
                    && options.DeleteAfterDays > options.ReminderAfterDays,
                    "Email verification housekeeping values are invalid.")
                .Validate(options => options.SecurityEventRetentionMonths > 0,
                    "EmailVerification:SecurityEventRetentionMonths must be positive.")
                .Validate(options => Uri.TryCreate(options.PublicApiBaseUrl, UriKind.Absolute, out _),
                    "EmailVerification:PublicApiBaseUrl must be absolute.")
                .Validate(options => Uri.TryCreate(options.PublicWebBaseUrl, UriKind.Absolute, out _),
                    "EmailVerification:PublicWebBaseUrl must be absolute.")
                .ValidateOnStart();

            services.AddHttpClient<IPasswordBreachChecker, PwnedPasswordChecker>(client =>
                client.BaseAddress = new Uri("https://api.pwnedpasswords.com/"));

            return services;
        }

        public static IServiceCollection AddSmtpEmail(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddOptions<SmtpEmailOptions>()
                .Bind(configuration.GetSection("Email"))
                .Validate(options => !string.IsNullOrWhiteSpace(options.Host),
                    "Email:Host must be configured.")
                .Validate(options => options.Port is > 0 and <= 65535,
                    "Email:Port must be between 1 and 65535.")
                .Validate(options => !string.IsNullOrWhiteSpace(options.SenderName),
                    "Email:SenderName must be configured.")
                .Validate(options => MailAddress.TryCreate(options.SenderAddress, out _),
                    "Email:SenderAddress must be a valid email address.")
                .Validate(options => string.IsNullOrWhiteSpace(options.Username)
                    || !string.IsNullOrWhiteSpace(options.Password),
                    "Email:Password must be configured when Email:Username is set.")
                .ValidateOnStart();

            services.AddTransient<IEmailSender, SmtpEmailSender>();
            return services;
        }

        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = JwtConfiguration.From(configuration);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(jwtSettings.SigningKey),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            return services;
        }
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<IIdentityService, IdentityService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IInternshipService, InternshipService>();
            services.AddScoped<IApplicationService, ApplicationService>();
            services.AddScoped<IInternshipLifecycleService, InternshipLifecycleService>();
            services.AddScoped<IStudentService, StudentService>();
            services.AddScoped<ICompanyService, CompanyService>();
            services.AddScoped<IAdminService, AdminService>();
               services.AddScoped<INotificationService, NotificationService>();
               services.AddScoped<INotificationService, NotificationService>();
services.AddScoped<IRecommendationNotifier, RecommendationNotifier>();
   services.AddScoped<ICompanyChatService, CompanyChatService>();
   services.AddScoped<ICompanyAgentToolsService, CompanyAgentToolsService>();
services.AddSingleton<INotificationPublisher, InternshipManagement.Api.Services.HttpNotificationPublisher>();
services.AddHttpClient("NotificationPush");
services.AddHostedService<InternshipManagement.Api.Jobs.RecommendationNotificationService>();
   services.AddHostedService<InternshipManagement.Api.Jobs.MaintenanceService>();

            // matching, CV analysis, skill gaps, career assistant, internship drafts
            services.AddScoped<IMatchingService, MatchingService>();
            services.AddScoped<IMatchExplanationService, MatchExplanationService>();
            services.AddScoped<ISkillGapService, SkillGapService>();
            services.AddScoped<ICvAnalysisService, CvAnalysisService>();
            services.AddScoped<IInternshipDraftService, InternshipDraftService>();
            services.AddScoped<ICareerChatService, CareerChatService>();

            // background AI jobs (one shared queue, one worker that drains it)
            services.AddScoped<IAiJobService, AiJobService>();
            services.AddSingleton<AiJobQueue>();
            services.AddSingleton<IAiJobQueue>(provider => provider.GetRequiredService<AiJobQueue>());
            services.AddHostedService<AiJobWorker>();

            // tools that n8n calls back into
            services.AddScoped<IAgentToolsService, AgentToolsService>();
            services.AddScoped<ICareerAgentToolsService, CareerAgentToolsService>();
            services.AddScoped<ICvAgentToolsService, CvAgentToolsService>();
            services.AddSingleton<IRunTokenService, RunTokenService>();

            // documents and reports
            services.AddScoped<IPrivateDocumentService, PrivateDocumentService>();
            services.AddScoped<IReportService, ReportService>();
            services.AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>();

            return services;
        }
        public static IServiceCollection AddValidation(this IServiceCollection services)
        {
            services.AddFluentValidationAutoValidation();
            services.AddFluentValidationClientsideAdapters();
            services.AddValidatorsFromAssemblyContaining<RegisterValidator>();

            return services;
        }

        public static IServiceCollection AddCorsPolicy(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });

                options.AddPolicy("AllowSpecific", policy =>
                {
                    policy.WithOrigins("http://localhost:3000", "http://localhost:4200")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            return services;
        }

        // ✅ NEW: .NET 10 OpenAPI without Swashbuckle
        public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddOpenApi();  // ✅ Built-in OpenAPI

            return services;
        }
    }
}
