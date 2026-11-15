using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace InternshipManagement.Web.Hubs
{
    /// <summary>
    /// The browser connects here to receive notifications the moment they are created. The hub has no methods the
    /// browser can call; it only receives pushes. Connections without a signed-in session, or from another
    /// website, are closed straight away.
    /// </summary>
    public sealed class NotificationHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            var http = Context.GetHttpContext();
            if (string.IsNullOrEmpty(Context.UserIdentifier) || !IsSameOrigin(http))
            {
                Context.Abort();
                return Task.CompletedTask;
            }

            return base.OnConnectedAsync();
        }

        private static bool IsSameOrigin(HttpContext? http)
        {
            if (http == null) return false;

            var origin = http.Request.Headers.Origin.ToString();
            if (string.IsNullOrEmpty(origin)) return true; // not sent by every client; the session cookie still has to match

            return Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                && string.Equals(uri.Authority, http.Request.Host.Value, StringComparison.OrdinalIgnoreCase);
        }
    }
}
