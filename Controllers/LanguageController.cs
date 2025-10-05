using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace ReverseMarket.Controllers
{
    public class LanguageController : Controller
    {
        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            if (string.IsNullOrEmpty(culture))
            {
                return BadRequest("Culture parameter is required");
            }

            // التحقق من صحة اللغة
            var validCultures = new[] { "ar", "en", "ku" };
            if (!validCultures.Contains(culture.ToLower()))
            {
                return BadRequest("Invalid culture");
            }

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    Path = "/",
                    SameSite = SameSiteMode.Lax
                }
            );

            // التأكد من وجود returnUrl صالح
            if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
            {
                returnUrl = "/";
            }

            return Redirect(returnUrl);
        }

        [HttpGet]
        public IActionResult GetCurrentLanguage()
        {
            var feature = HttpContext.Features.Get<IRequestCultureFeature>();
            var currentCulture = feature?.RequestCulture.Culture.Name ?? "ar";

            return Json(new { language = currentCulture });
        }
    }
}