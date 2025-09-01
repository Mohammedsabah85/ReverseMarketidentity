namespace ReverseMarket.Models
{
    public class WhatsAppSettings
    {
        public string ApiUrl { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public string DefaultPhone { get; set; } = "";
        public bool EnableNotifications { get; set; } = true;
    }
}