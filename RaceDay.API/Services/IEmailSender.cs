using System.Net;
using System.Net.Mail;

namespace RaceDay.API.Services
{
    /// <summary>Sends plain-text emails (used for password reset links).</summary>
    public interface IEmailSender
    {
        /// <summary>
        /// Sends the email. Returns false when no SMTP server is configured
        /// (so the caller knows nothing was actually delivered).
        /// </summary>
        Task<bool> SendAsync(string toEmail, string subject, string body);
    }

    /// <summary>
    /// Sends email through an SMTP server configured under the "Smtp" section
    /// (Host, Port, Username, Password, FromAddress, EnableSsl).
    /// If Host is empty nothing is sent and the link is only logged.
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendAsync(string toEmail, string subject, string body)
        {
            var host = _configuration["Smtp:Host"];
            if (string.IsNullOrWhiteSpace(host))
                return false;

            try
            {
                var port = int.TryParse(_configuration["Smtp:Port"], out var p) ? p : 587;
                var from = _configuration["Smtp:FromAddress"] ?? "no-reply@raceday.local";
                var enableSsl = !bool.TryParse(_configuration["Smtp:EnableSsl"], out var ssl) || ssl;

                using var client = new SmtpClient(host, port) { EnableSsl = enableSsl };

                var username = _configuration["Smtp:Username"];
                if (!string.IsNullOrWhiteSpace(username))
                    client.Credentials = new NetworkCredential(username, _configuration["Smtp:Password"]);

                using var message = new MailMessage(from, toEmail, subject, body);
                await client.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                // Never reveal mail problems to the caller (it would leak whether the email exists).
                _logger.LogError(ex, "Could not send password reset email.");
                return false;
            }
        }
    }
}
