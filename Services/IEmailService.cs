namespace ReverseMarket.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string body);
        Task<bool> SendVerificationEmailAsync(string email, string verificationCode);
        Task<bool> SendNotificationEmailAsync(string email, string title, string message);
    }
}