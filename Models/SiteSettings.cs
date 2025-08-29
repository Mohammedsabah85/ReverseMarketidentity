// Models/SiteSettings.cs
namespace ReverseMarket.Models
{
    public class SiteSettings
    {
        public int Id { get; set; }

        public string? SiteLogo { get; set; }

        public string? AboutUs { get; set; }

        public string? ContactPhone { get; set; }

        public string? ContactWhatsApp { get; set; }

        public string? ContactEmail { get; set; }

        public string? FacebookUrl { get; set; }

        public string? InstagramUrl { get; set; }

        public string? TwitterUrl { get; set; }

        public string? YouTubeUrl { get; set; }

        public string? PrivacyPolicy { get; set; }

        public string? TermsOfUse { get; set; }

        public string? CopyrightInfo { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
