using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Models;
using ReverseMarket.Areas.Admin.Models; // Add this line
using Microsoft.AspNetCore.Hosting;

using System.ComponentModel.DataAnnotations;

namespace ReverseMarket.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AdvertisementsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AdvertisementsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var advertisements = await _context.Advertisements
                .OrderBy(a => a.Type)
                .ThenBy(a => a.DisplayOrder)
                .ToListAsync();

            return View(advertisements);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateAdvertisementViewModel model)
        {
            if (ModelState.IsValid)
            {
                string imagePath = "";

                if (model.Image != null)
                {
                    imagePath = await SaveImageAsync(model.Image);
                }

                var advertisement = new Advertisement
                {
                    Title = model.Title,
                    Description = model.Description,
                    ImagePath = imagePath,
                    LinkUrl = model.LinkUrl,
                    Type = model.Type,
                    DisplayOrder = model.DisplayOrder,
                    IsActive = model.IsActive,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate
                };

                _context.Advertisements.Add(advertisement);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index");
            }

            return View(model);
        }

        private async Task<string> SaveImageAsync(IFormFile image)
        {
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "advertisements");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            return $"/uploads/advertisements/{fileName}";
        }
    

[HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var advertisement = await _context.Advertisements.FindAsync(id);
            if (advertisement != null)
            {
                // حذف الصورة من الخادم
                if (!string.IsNullOrEmpty(advertisement.ImagePath))
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, advertisement.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.Advertisements.Remove(advertisement);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}