using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReverseMarket.CustomWhatsappService;
using ReverseMarket.Data;
using ReverseMarket.Models;
using ReverseMarket.Services;
using System.Diagnostics;
using Twilio.Types;
using static System.Net.Mime.MediaTypeNames;


using Microsoft.Extensions.Localization;

using ReverseMarket.Resources;


namespace ReverseMarket.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        TwilioWhatsapp _twilio=new TwilioWhatsapp();
        private readonly IStringLocalizer<SharedResource> _localizer;


        public HomeController(ApplicationDbContext context, IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            // _twilio.SendWhatsAppMessage("+9647801861182", $"code is {"99998520"} do not share");
            
            // الحصول على إعدادات الموقع
            var siteSettings = await _context.SiteSettings.FirstOrDefaultAsync();
            ViewBag.SiteSettings = siteSettings;

            var model = new ReverseMarket.Models.HomeViewModel
            {
                Advertisements = await _context.Advertisements
                    .Where(a => a.IsActive &&
                           a.StartDate <= DateTime.Now &&
                           (a.EndDate == null || a.EndDate >= DateTime.Now))
                    .OrderBy(a => a.DisplayOrder)
                    .ThenBy(a => a.CreatedAt)
                    .ToListAsync(),

                Categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .Take(8)
                    .ToListAsync(),

                RecentRequests = await _context.Requests
                    .Where(r => r.Status == RequestStatus.Approved)
                    .Include(r => r.Category)
                    .Include(r => r.Images)
                    .OrderByDescending(r => r.ApprovedAt)
                    .Take(12)
                    .ToListAsync(),

                SiteSettings = siteSettings
            };

            return View(model);
        }

        public async Task<IActionResult> About()
        {
            var siteSettings = await _context.SiteSettings.FirstOrDefaultAsync();
            ViewBag.SiteSettings = siteSettings;
            return View();
        }

        public async Task<IActionResult> Contact()
        {
            var siteSettings = await _context.SiteSettings.FirstOrDefaultAsync();
            ViewBag.SiteSettings = siteSettings;
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Contact(ContactFormModel model)
        {
            if (ModelState.IsValid)
            {
                // هنا يمكنك إضافة منطق إرسال البريد الإلكتروني أو حفظ الرسالة في قاعدة البيانات

                TempData["SuccessMessage"] = _localizer["MessageSentSuccess"];
                return RedirectToAction(nameof(Contact));
            }

            return View(model);
        }

        public IActionResult Terms()
        {
            return View();
        }
        public async Task<IActionResult> Privacy()
        {
            var siteSettings = await _context.SiteSettings.FirstOrDefaultAsync();
            ViewBag.SiteSettings = siteSettings;
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
    public class ContactFormModel
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
    }
}