// Controllers/DiagnosticController.cs - الإصدار المُصحح
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using ReverseMarket.Services;
using System.Globalization;

namespace ReverseMarket.Controllers
{
    public class DiagnosticControllerlng : Controller
    {
        private readonly ILanguageService _languageService;
        private readonly IOptions<RequestLocalizationOptions> _localizationOptions;
        private readonly ILogger<DiagnosticController> _logger;

        public DiagnosticControllerlng(
            ILanguageService languageService,
            IOptions<RequestLocalizationOptions> localizationOptions,
            ILogger<DiagnosticController> logger)
        {
            _languageService = languageService;
            _localizationOptions = localizationOptions;
            _logger = logger;
        }

        public IActionResult LanguageDiagnostic()
        {
            var diagnostic = new LanguageDiagnosticInfo();

            try
            {
                // معلومات اللغة الحالية
                diagnostic.CurrentLanguage = _languageService.GetCurrentLanguage();
                diagnostic.CurrentDirection = _languageService.GetDirection();
                diagnostic.SupportedLanguages = _languageService.GetSupportedLanguages();

                // معلومات Culture
                diagnostic.CurrentCulture = CultureInfo.CurrentCulture.Name;
                diagnostic.CurrentUICulture = CultureInfo.CurrentUICulture.Name;
                diagnostic.ThreadCulture = Thread.CurrentThread.CurrentCulture.Name;
                diagnostic.ThreadUICulture = Thread.CurrentThread.CurrentUICulture.Name;

                // معلومات RequestCultureFeature
                var feature = HttpContext.Features.Get<IRequestCultureFeature>();
                if (feature != null)
                {
                    diagnostic.RequestCulture = feature.RequestCulture.Culture.Name;
                    diagnostic.RequestUICulture = feature.RequestCulture.UICulture.Name;
                    diagnostic.ProviderResultCulture = feature.Provider?.GetType().Name;
                }

                // فحص Cookies
                var cookieValue = Request.Cookies[CookieRequestCultureProvider.DefaultCookieName];
                diagnostic.CookieValue = cookieValue;

                if (!string.IsNullOrEmpty(cookieValue))
                {
                    try
                    {
                        var requestCulture = CookieRequestCultureProvider.ParseCookieValue(cookieValue);
                        if (requestCulture != null)
                        {
                            // إصلاح خطأ StringSegment - نفس الطريقة المستخدمة في LanguageService
                            diagnostic.CookieCulture = null;
                            diagnostic.CookieUICulture = null;

                            if (requestCulture.Cultures != null && requestCulture.Cultures.Count > 0)
                            {
                                diagnostic.CookieCulture = requestCulture.Cultures[0].Value;
                            }

                            if (requestCulture.UICultures != null && requestCulture.UICultures.Count > 0)
                            {
                                diagnostic.CookieUICulture = requestCulture.UICultures[0].Value;
                            }

                            diagnostic.CookieParseSuccess = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        diagnostic.CookieParseError = ex.Message;
                        diagnostic.CookieParseSuccess = false;
                    }
                }

                // معلومات Accept-Language Header
                diagnostic.AcceptLanguageHeader = Request.Headers["Accept-Language"].FirstOrDefault();

                // إعدادات Localization
                var options = _localizationOptions.Value;
                diagnostic.DefaultCulture = options.DefaultRequestCulture.Culture.Name;
                diagnostic.DefaultUICulture = options.DefaultRequestCulture.UICulture.Name;
                diagnostic.SupportedCultures = options.SupportedCultures?.Select(c => c.Name).ToList() ?? new List<string>();
                diagnostic.SupportedUICultures = options.SupportedUICultures?.Select(c => c.Name).ToList() ?? new List<string>();
                diagnostic.CultureProviders = options.RequestCultureProviders.Select(p => p.GetType().Name).ToList();

                // فحص صحة النظام
                diagnostic.Issues = new List<string>();

                // فحص 1: تطابق اللغات
                if (diagnostic.CurrentLanguage != diagnostic.RequestCulture?.Split('-')[0])
                {
                    diagnostic.Issues.Add($"Current Language ({diagnostic.CurrentLanguage}) doesn't match Request Culture ({diagnostic.RequestCulture})");
                }

                // فحص 2: وجود Cookie
                if (string.IsNullOrEmpty(diagnostic.CookieValue))
                {
                    diagnostic.Issues.Add("No language cookie found");
                }

                // فحص 3: نجاح تحليل Cookie
                if (!diagnostic.CookieParseSuccess && !string.IsNullOrEmpty(diagnostic.CookieValue))
                {
                    diagnostic.Issues.Add($"Cookie parse failed: {diagnostic.CookieParseError}");
                }

                // فحص 4: إعدادات Localization
                if (options.RequestCultureProviders.Count == 0)
                {
                    diagnostic.Issues.Add("No culture providers configured");
                }

                // فحص 5: تطابق اللغة مع المدعومة
                if (!_languageService.IsLanguageSupported(diagnostic.CurrentLanguage))
                {
                    diagnostic.Issues.Add($"Current language ({diagnostic.CurrentLanguage}) is not in supported languages list");
                }

                diagnostic.Success = diagnostic.Issues.Count == 0;
            }
            catch (Exception ex)
            {
                diagnostic.Success = false;
                diagnostic.Issues = new List<string> { $"Diagnostic failed: {ex.Message}" };
                _logger.LogError(ex, "Language diagnostic failed");
            }

            return View(diagnostic);
        }

        [HttpPost]
        public IActionResult TestLanguageChange(string culture)
        {
            try
            {
                if (string.IsNullOrEmpty(culture) || !_languageService.IsLanguageSupported(culture))
                {
                    return BadRequest(new { success = false, message = "Invalid culture" });
                }

                _languageService.SetLanguage(culture);

                return Ok(new
                {
                    success = true,
                    message = "Language changed successfully",
                    newCulture = culture,
                    cookieSet = Response.Headers.ContainsKey("Set-Cookie")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Test language change failed for culture: {Culture}", culture);
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult ClearLanguageCookie()
        {
            try
            {
                Response.Cookies.Delete(CookieRequestCultureProvider.DefaultCookieName);
                return Ok(new { success = true, message = "Language cookie cleared" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Action إضافي لاختبار معلومات Cookie مفصلة
        public IActionResult CookieInfo()
        {
            var info = new
            {
                AllCookies = Request.Cookies.ToDictionary(c => c.Key, c => c.Value),
                CultureCookie = Request.Cookies[CookieRequestCultureProvider.DefaultCookieName],
                Headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
            };

            return Json(info);
        }
    }

    public class LanguageDiagnosticInfo
    {
        public bool Success { get; set; }
        public List<string> Issues { get; set; } = new();

        // معلومات اللغة الحالية
        public string CurrentLanguage { get; set; } = "";
        public string CurrentDirection { get; set; } = "";
        public List<LanguageOption> SupportedLanguages { get; set; } = new();

        // معلومات Culture
        public string CurrentCulture { get; set; } = "";
        public string CurrentUICulture { get; set; } = "";
        public string ThreadCulture { get; set; } = "";
        public string ThreadUICulture { get; set; } = "";

        // معلومات Request
        public string? RequestCulture { get; set; }
        public string? RequestUICulture { get; set; }
        public string? ProviderResultCulture { get; set; }

        // معلومات Cookie
        public string? CookieValue { get; set; }
        public string? CookieCulture { get; set; }
        public string? CookieUICulture { get; set; }
        public bool CookieParseSuccess { get; set; }
        public string? CookieParseError { get; set; }

        // معلومات Headers
        public string? AcceptLanguageHeader { get; set; }

        // إعدادات Localization
        public string DefaultCulture { get; set; } = "";
        public string DefaultUICulture { get; set; } = "";
        public List<string> SupportedCultures { get; set; } = new();
        public List<string> SupportedUICultures { get; set; } = new();
        public List<string> CultureProviders { get; set; } = new();
    }
}