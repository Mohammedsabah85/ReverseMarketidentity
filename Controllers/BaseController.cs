using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Models;
using ReverseMarket.Services;

namespace ReverseMarket.Controllers
{
    public class BaseController : Controller
    {
        protected readonly ApplicationDbContext _context;

        public BaseController(ApplicationDbContext context)
        {
            _context = context;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // تحميل إعدادات الموقع وتمريرها لجميع الصفحات
            var siteSettings = await _context.SiteSettings.FirstOrDefaultAsync();
            ViewBag.SiteSettings = siteSettings;

            await next();
        }
    }
}

