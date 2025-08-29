// Controllers/LanguageController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Localization;
using ReverseMarket.Services;

namespace ReverseMarket.Controllers
{
    public class LanguageController : Controller
    {
        private readonly ILanguageService _languageService;
        private readonly ILogger<LanguageController> _logger;

        public LanguageController(ILanguageService languageService, ILogger<LanguageController> logger)
        {
            _languageService = languageService;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetLanguage(string culture, string returnUrl = "/")
        {
            try
            {
                _logger.LogInformation("Language change requested to: {Culture}", culture);

                // التحقق من صحة اللغة
                if (string.IsNullOrEmpty(culture) || !_languageService.IsLanguageSupported(culture))
                {
                    _logger.LogWarning("Unsupported language requested: {Culture}", culture);
                    TempData["ErrorMessage"] = "اللغة المطلوبة غير مدعومة";
                    return LocalRedirect(returnUrl);
                }

                // تحديد اللغة
                _languageService.SetLanguage(culture);

                _logger.LogInformation("Language successfully changed to: {Culture}", culture);
                TempData["SuccessMessage"] = "تم تغيير اللغة بنجاح";

                // التأكد من أن returnUrl آمن
                if (!Url.IsLocalUrl(returnUrl))
                {
                    returnUrl = "/";
                }

                return LocalRedirect(returnUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing language to {Culture}", culture);
                TempData["ErrorMessage"] = "حدث خطأ أثناء تغيير اللغة";
                return LocalRedirect(returnUrl);
            }
        }

        // API endpoint لـ AJAX requests
        [HttpPost]
        public IActionResult SetLanguageAjax([FromBody] SetLanguageRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Culture) || !_languageService.IsLanguageSupported(request.Culture))
                {
                    return BadRequest(new { success = false, message = "اللغة المطلوبة غير مدعومة" });
                }

                _languageService.SetLanguage(request.Culture);

                return Ok(new { success = true, message = "تم تغيير اللغة بنجاح" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing language via AJAX to {Culture}", request.Culture);
                return BadRequest(new { success = false, message = "حدث خطأ أثناء تغيير اللغة" });
            }
        }

        public class SetLanguageRequest
        {
            public string Culture { get; set; } = "";
        }
    }
}