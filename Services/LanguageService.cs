using Microsoft.AspNetCore.Localization;
using System.Globalization;

namespace ReverseMarket.Services
{
    public class LanguageService : ILanguageService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly List<LanguageOption> _supportedLanguages;

        public LanguageService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _supportedLanguages = new List<LanguageOption>
            {
                new LanguageOption
                {
                    Code = "ar",
                    Name = "العربية",
                    NativeName = "العربية",
                    Direction = "rtl",
                    FlagIcon = "flag-icon-iq"
                },
                new LanguageOption
                {
                    Code = "en",
                    Name = "English",
                    NativeName = "English",
                    Direction = "ltr",
                    FlagIcon = "flag-icon-us"
                }
            };
        }

        public string GetCurrentLanguage()
        {
            var culture = CultureInfo.CurrentCulture.Name;
            if (culture.StartsWith("ar"))
                return "ar";
            return "en";
        }

        public string GetDirection()
        {
            var currentLang = GetCurrentLanguage();
            var langOption = _supportedLanguages.FirstOrDefault(l => l.Code == currentLang);
            return langOption?.Direction ?? "rtl";
        }

        public List<LanguageOption> GetSupportedLanguages()
        {
            return _supportedLanguages;
        }

        public bool IsLanguageSupported(string language)
        {
            return _supportedLanguages.Any(l => l.Code == language);
        }

        public void SetLanguage(string language)
        {
            if (!IsLanguageSupported(language))
                return;

            var context = _httpContextAccessor.HttpContext;
            if (context == null) return;

            var culture = language == "ar" ? "ar-IQ" : "en-US";
            var requestCulture = new RequestCulture(culture);

            context.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(requestCulture),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    HttpOnly = false,
                    Secure = context.Request.IsHttps
                });
        }
    }
}