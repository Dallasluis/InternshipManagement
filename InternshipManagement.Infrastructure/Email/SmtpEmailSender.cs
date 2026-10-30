using InternshipManagement.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace InternshipManagement.Infrastructure.Email
{
    public sealed class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpEmailOptions _options;

        public SmtpEmailSender(IOptions<SmtpEmailOptions> options)
        {
            _options = options.Value;
        }

        public async Task SendEmailAsync(
            string recipient,
            string subject,
            string body,
            CancellationToken cancellationToken = default)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.SenderName, _options.SenderAddress));
            message.To.Add(MailboxAddress.Parse(recipient));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            var secureSocketOptions = _options.UseSsl
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.None;

            await client.ConnectAsync(_options.Host, _options.Port, secureSocketOptions, cancellationToken);
            if (!string.IsNullOrWhiteSpace(_options.Username))
                await client.AuthenticateAsync(_options.Username, _options.Password ?? string.Empty, cancellationToken);

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
    }
}
