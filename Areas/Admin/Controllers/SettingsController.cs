using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Models;

namespace ReverseMarket.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public SettingsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _context.SiteSettings.FirstOrDefaultAsync() ?? new SiteSettings();
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SiteSettings model, IFormFile? logoFile)
        {
            if (ModelState.IsValid)
            {
                var settings = await _context.SiteSettings.FirstOrDefaultAsync();

                if (settings == null)
                {
                    settings = new SiteSettings();
                    _context.SiteSettings.Add(settings);
                }

                if (logoFile != null)
                {
                    var logoPath = await SaveLogoAsync(logoFile);
                    settings.SiteLogo = logoPath;
                }

                settings.AboutUs = model.AboutUs;
                settings.ContactPhone = model.ContactPhone;
                settings.ContactWhatsApp = model.ContactWhatsApp;
                settings.ContactEmail = model.ContactEmail;
                settings.FacebookUrl = model.FacebookUrl;
                settings.InstagramUrl = model.InstagramUrl;
                settings.TwitterUrl = model.TwitterUrl;
                settings.YouTubeUrl = model.YouTubeUrl;
                settings.PrivacyPolicy = model.PrivacyPolicy;
                settings.TermsOfUse = model.TermsOfUse;
                settings.CopyrightInfo = model.CopyrightInfo;
                settings.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "تم حفظ الإعدادات بنجاح";
                return RedirectToAction("Index");
            }

            return View(model);
        }

        private async Task<string> SaveLogoAsync(IFormFile logo)
        {
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "site");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = "logo" + Path.GetExtension(logo.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await logo.CopyToAsync(fileStream);
            }

            return $"/uploads/site/{fileName}";
        }
    }
}
