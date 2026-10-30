namespace InternshipManagement.Web.Services
{
    /// <summary>
    /// Tells the API which browser a request came from. Without this the API only ever sees the Web server's address,
    /// so its per-address login limit would be shared by every user and its security log would show one address for everyone.
    /// The API must be set to trust this header from the Web server (see the stage 4 notes).
    /// </summary>
    public sealed class ClientIpForwardingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _accessor;

        public ClientIpForwardingHandler(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var ip = _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(ip) && !request.Headers.Contains("X-Forwarded-For"))
                request.Headers.TryAddWithoutValidation("X-Forwarded-For", ip);

            return base.SendAsync(request, cancellationToken);
        }
    }
}
