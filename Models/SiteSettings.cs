// Models/SiteSettings.cs
namespace ReverseMarket.Models
{
    public class SiteSettings
    {
        public int Id { get; set; }

        public string? SiteLogo { get; set; }

        // About Us - متعدد اللغات
        public string? AboutUs { get; set; }
        public string? AboutUsEn { get; set; }
        public string? AboutUsKu { get; set; }

        // Contact Information
        public string? ContactPhone { get; set; }
        public string? ContactWhatsApp { get; set; }
        public string? ContactEmail { get; set; }

        // Social Media
        public string? FacebookUrl { get; set; }
        public string? InstagramUrl { get; set; }
        public string? TwitterUrl { get; set; }
        public string? YouTubeUrl { get; set; }

        // Privacy Policy - متعدد اللغات
        public string? PrivacyPolicy { get; set; }
        public string? PrivacyPolicyEn { get; set; }
        public string? PrivacyPolicyKu { get; set; }

        // Terms of Use - متعدد اللغات
        public string? TermsOfUse { get; set; }
        public string? TermsOfUseEn { get; set; }
        public string? TermsOfUseKu { get; set; }

        // Copyright Info - متعدد اللغات
        public string? CopyrightInfo { get; set; }
        public string? CopyrightInfoEn { get; set; }
        public string? CopyrightInfoKu { get; set; }

        // Intellectual Property - متعدد اللغات
        public string? IntellectualProperty { get; set; }
        public string? IntellectualPropertyEn { get; set; }
        public string? IntellectualPropertyKu { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Helper Methods للحصول على المحتوى حسب اللغة
        public string GetAboutUs(string language)
        {
            return language switch
            {
                "en" => AboutUsEn ?? AboutUs ?? string.Empty,
                "ku" => AboutUsKu ?? AboutUs ?? string.Empty,
                _ => AboutUs ?? string.Empty
            };
        }

        public string GetPrivacyPolicy(string language)
        {
            return language switch
            {
                "en" => PrivacyPolicyEn ?? PrivacyPolicy ?? string.Empty,
                "ku" => PrivacyPolicyKu ?? PrivacyPolicy ?? string.Empty,
                _ => PrivacyPolicy ?? string.Empty
            };
        }

        public string GetTermsOfUse(string language)
        {
            return language switch
            {
                "en" => TermsOfUseEn ?? TermsOfUse ?? string.Empty,
                "ku" => TermsOfUseKu ?? TermsOfUse ?? string.Empty,
                _ => TermsOfUse ?? string.Empty
            };
        }

        public string GetCopyrightInfo(string language)
        {
            return language switch
            {
                "en" => CopyrightInfoEn ?? CopyrightInfo ?? string.Empty,
                "ku" => CopyrightInfoKu ?? CopyrightInfo ?? string.Empty,
                _ => CopyrightInfo ?? string.Empty
            };
        }

        public string GetIntellectualProperty(string language)
        {
            return language switch
            {
                "en" => IntellectualPropertyEn ?? IntellectualProperty ?? string.Empty,
                "ku" => IntellectualPropertyKu ?? IntellectualProperty ?? string.Empty,
                _ => IntellectualProperty ?? string.Empty
            };
        }
    }
}