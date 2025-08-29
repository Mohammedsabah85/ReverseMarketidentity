using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Models;
using System.Diagnostics;

namespace ReverseMarket.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
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
}