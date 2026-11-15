using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace InternshipManagement.Web.Hubs
{
    /// <summary>Identifies a SignalR connection by the signed-in user stored in the server-side session.</summary>
    public sealed class SessionUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            var id = connection.GetHttpContext()?.Session.GetString("UserId");
            return string.IsNullOrWhiteSpace(id) ? null : id;
        }
    }
}
