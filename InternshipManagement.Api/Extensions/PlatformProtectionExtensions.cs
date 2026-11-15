using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;

namespace InternshipManagement.Api.Extensions
{
    public static class PlatformProtectionExtensions
    {
        /// <summary>
        /// A limit on every request (per signed-in user, or per address for anonymous visitors), a clear 429 answer,
        /// and trust for the real visitor address sent by the Web project or a proxy.
        /// Call it AFTER AddRateLimiter, so the answer below replaces the default empty 429.
        /// Settings: RateLimiting:RequestsPerMinute (default 120) and ForwardedHeaders:KnownProxies (list of proxy addresses).
        /// </summary>
        public static IServiceCollection AddPlatformProtection(this IServiceCollection services, IConfiguration config)
        {
            var perMinute = Math.Max(config.GetValue("RateLimiting:RequestsPerMinute", 120), 10);

            services.Configure<RateLimiterOptions>(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    var userId = context.User.FindFirst("userId")?.Value;
                    var key = !string.IsNullOrEmpty(userId)
                        ? "u:" + userId
                        : "ip:" + (context.Connection.RemoteIpAddress?.ToString() ?? "unknown");

                    return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = perMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
                });

                options.OnRejected = async (context, cancellationToken) =>
                {
                    var http = context.HttpContext;
                    var seconds = 60;
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                        seconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));

                    http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    http.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);

                    var wait = seconds >= 120
                        ? $"{(int)Math.Ceiling(seconds / 60.0)} minutes"
                        : seconds == 1 ? "1 second" : $"{seconds} seconds";

                    await http.Response.WriteAsJsonAsync(new
                    {
                        error = $"You are doing that too fast. Please wait {wait} and try again.",
                        retryAfterSeconds = seconds
                    }, cancellationToken);
                };
            });

            // Loopback is trusted by default (local Web and API). Add the Web server or proxy addresses for a real deployment.
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.ForwardLimit = 2;

                foreach (var proxy in config.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? Array.Empty<string>())
                    if (IPAddress.TryParse(proxy, out var address))
                        options.KnownProxies.Add(address);
            });

            return services;
        }
    }
}
