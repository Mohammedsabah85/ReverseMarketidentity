using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;

namespace ReverseMarket.Services
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
                message.To.Add(new MailboxAddress("", to));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = body
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", to);
                return false;
            }
        }

        public async Task<bool> SendVerificationEmailAsync(string email, string verificationCode)
        {
            var subject = "تأكيد البريد الإلكتروني - السوق العكسي";
            var body = $@"
                <div style='direction: rtl; font-family: Arial, sans-serif;'>
                    <h2>مرحباً بك في السوق العكسي</h2>
                    <p>لتأكيد بريدك الإلكتروني، يرجى استخدام الرمز التالي:</p>
                    <div style='background-color: #f8f9fa; padding: 20px; text-align: center; font-size: 24px; font-weight: bold; color: #2c5aa0;'>
                        {verificationCode}
                    </div>
                    <p>هذا الرمز صالح لمدة 15 دقيقة فقط.</p>
                    <hr>
                    <p style='font-size: 12px; color: #666;'>
                        إذا لم تطلب هذا الرمز، يرجى تجاهل هذه الرسالة.
                    </p>
                </div>";

            return await SendEmailAsync(email, subject, body);
        }

        public async Task<bool> SendNotificationEmailAsync(string email, string title, string message)
        {
            var subject = $"{title} - السوق العكسي";
            var body = $@"
                <div style='direction: rtl; font-family: Arial, sans-serif;'>
                    <h2>{title}</h2>
                    <p>{message}</p>
                    <hr>
                    <p style='font-size: 12px; color: #666;'>
                        هذه رسالة آلية من السوق العكسي
                    </p>
                </div>";

            return await SendEmailAsync(email, subject, body);
        }
    }
}