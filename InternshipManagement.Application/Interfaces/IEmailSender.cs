namespace InternshipManagement.Application.Interfaces
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default);
    }
}
