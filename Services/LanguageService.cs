// Services/LanguageService.cs - الإصدار المُصحح
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Localization;
using ReverseMarket.Resources;
using System.Globalization;

namespace ReverseMarket.Services
{
    public class LanguageService : ILanguageService
    {
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<LanguageService> _logger;

        public LanguageService(
            IStringLocalizer<SharedResource> localizer,
            IHttpContextAccessor httpContextAccessor,
            ILogger<LanguageService> logger)
        {
            _localizer = localizer;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public string GetCurrentLanguage()
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;
                if (context != null)
                {
                    // محاولة الحصول على اللغة من RequestCultureFeature
                    var feature = context.Features.Get<IRequestCultureFeature>();
                    if (feature?.RequestCulture?.Culture != null)
                    {
                        var culture = feature.RequestCulture.Culture.TwoLetterISOLanguageName;
                        _logger.LogDebug("Current language from RequestCultureFeature: {Culture}", culture);
                        return culture;
                    }

                    // محاولة الحصول على اللغة من الكوكيز
                    var cookieValue = context.Request.Cookies[CookieRequestCultureProvider.DefaultCookieName];
                    if (!string.IsNullOrEmpty(cookieValue))
                    {
                        try
                        {
                            var requestCulture = CookieRequestCultureProvider.ParseCookieValue(cookieValue);
                            if (requestCulture != null)
                            {
                                // إصلاح خطأ StringSegment - استخدام Value بدلاً من null-conditional operator
                                string culture = "ar"; // قيمة افتراضية

                                if (requestCulture.Cultures != null && requestCulture.Cultures.Count > 0)
                                {
                                    culture = requestCulture.Cultures[0].Value;
                                }
                                else if (requestCulture.UICultures != null && requestCulture.UICultures.Count > 0)
                                {
                                    culture = requestCulture.UICultures[0].Value;
                                }

                                // استخراج الجزء الأول من الثقافة (مثل "ar" من "ar-IQ")
                                var languageCode = culture.Split('-')[0];
                                _logger.LogDebug("Current language from cookie: {Culture}", languageCode);
                                return languageCode;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse culture cookie: {CookieValue}", cookieValue);
                        }
                    }

                    // محاولة الحصول على اللغة من Accept-Language header
                    var acceptLanguageHeader = context.Request.Headers["Accept-Language"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(acceptLanguageHeader))
                    {
                        var preferredLanguage = acceptLanguageHeader.Split(',')[0].Split('-')[0];
                        if (IsLanguageSupported(preferredLanguage))
                        {
                            _logger.LogDebug("Current language from Accept-Language: {Culture}", preferredLanguage);
                            return preferredLanguage;
                        }
                    }
                }

                // استخدام CurrentCulture كبديل
                var currentCulture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                _logger.LogDebug("Current language from CultureInfo: {Culture}", currentCulture);
                return currentCulture;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current language, defaulting to Arabic");
                return "ar";
            }
        }

        public string GetDirection()
        {
            try
            {
                var currentLanguage = GetCurrentLanguage();
                var direction = currentLanguage == "ar" || currentLanguage == "ku" ? "rtl" : "ltr";
                _logger.LogDebug("Direction for {Language}: {Direction}", currentLanguage, direction);
                return direction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting direction, defaulting to RTL");
                return "rtl";
            }
        }

        public List<LanguageOption> GetSupportedLanguages()
        {
            return new List<LanguageOption>
            {
                new LanguageOption
                {
                    Code = "ar",
                    Name = "العربية",
                    NativeName = "العربية",
                    Flag = "🇮🇶",
                    Direction = "rtl"
                },
                new LanguageOption
                {
                    Code = "en",
                    Name = "English",
                    NativeName = "English",
                    Flag = "🇺🇸",
                    Direction = "ltr"
                },
                new LanguageOption
                {
                    Code = "ku",
                    Name = "کوردی",
                    NativeName = "کوردی",
                    Flag = "🏴",
                    Direction = "rtl"
                }
            };
        }

        public void SetLanguage(string languageCode)
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
            {
                _logger.LogWarning("HttpContext is null, cannot set language");
                return;
            }

            try
            {
                // التحقق من أن اللغة مدعومة
                if (!IsLanguageSupported(languageCode))
                {
                    _logger.LogWarning("Unsupported language code: {LanguageCode}", languageCode);
                    throw new ArgumentException($"Unsupported language: {languageCode}");
                }

                // إنشاء Culture مع اللغة والمنطقة المناسبة
                var cultureInfo = languageCode switch
                {
                    "ar" => new CultureInfo("ar-IQ"),
                    "en" => new CultureInfo("en-US"),
                    "ku" => new CultureInfo("ku-IQ"),
                    _ => new CultureInfo("ar-IQ")
                };

                // إنشاء RequestCulture
                var requestCulture = new RequestCulture(cultureInfo, cultureInfo);

                // حفظ الكوكيز
                var cookieValue = CookieRequestCultureProvider.MakeCookieValue(requestCulture);
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    HttpOnly = false, // مهم: false للسماح بالوصول من JavaScript
                    Secure = context.Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    IsEssential = true // مهم للامتثال لـ GDPR
                };

                context.Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    cookieValue,
                    cookieOptions);

                // تحديث Culture للطلب الحالي
                Thread.CurrentThread.CurrentCulture = cultureInfo;
                Thread.CurrentThread.CurrentUICulture = cultureInfo;
                CultureInfo.CurrentCulture = cultureInfo;
                CultureInfo.CurrentUICulture = cultureInfo;

                // تحديث RequestCultureFeature
                var feature = context.Features.Get<IRequestCultureFeature>();
                if (feature != null)
                {
                    var newFeature = new RequestCultureFeature(requestCulture, null);
                    context.Features.Set<IRequestCultureFeature>(newFeature);
                }

                _logger.LogInformation("Language successfully set to: {LanguageCode} ({Culture})",
                    languageCode, cultureInfo.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting language to {LanguageCode}", languageCode);
                throw;
            }
        }

        public bool IsLanguageSupported(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                return false;

            var supportedLanguages = GetSupportedLanguages();
            return supportedLanguages.Any(l => l.Code.Equals(languageCode, StringComparison.OrdinalIgnoreCase));
        }

        public LanguageOption? GetLanguageInfo(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
                return null;

            var supportedLanguages = GetSupportedLanguages();
            return supportedLanguages.FirstOrDefault(l => l.Code.Equals(languageCode, StringComparison.OrdinalIgnoreCase));
        }
    }
}