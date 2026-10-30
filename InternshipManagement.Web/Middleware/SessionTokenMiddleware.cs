using System.Net;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace InternshipManagement.Web.Middleware
{
    /// <summary>
    /// Runs before every page: renews the API token when it is about to expire, and turns an API "401 Unauthorized"
    /// (session ended on the server, account suspended) into a clean redirect to the login page instead of an error page.
    /// </summary>
    public sealed class SessionTokenMiddleware
    {
        private const string LoginPath = "/Account/Login";
        private readonly RequestDelegate _next;

        public SessionTokenMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, TokenRefreshCoordinator coordinator)
        {
            var outcome = await coordinator.EnsureFreshAsync(context.Session, force: false, context.RequestAborted);
            if (outcome == RefreshOutcome.Expired)
                Notify(context);

            try
            {
                await _next(context);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized && !context.Response.HasStarted)
            {
                context.Session.Clear();
                Notify(context);
                await Reject(context);
            }
        }

        private static Task Reject(HttpContext context)
        {
            // Scripts (they send the anti-forgery header) get a 401 they can handle; pages get redirected.
            if (context.Request.Headers.ContainsKey("RequestVerificationToken"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(LoginPath);
            return Task.CompletedTask;
        }

        private static void Notify(HttpContext context)
        {
            var factory = context.RequestServices.GetService<ITempDataDictionaryFactory>();
            if (factory == null)
                return;

            var tempData = factory.GetTempData(context);
            tempData["StatusMessage"] = "Your session has ended. Please sign in again.";
            tempData.Save();
        }
    }
}
