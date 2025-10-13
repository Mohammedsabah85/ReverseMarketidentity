using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Models.Identity;

namespace ReverseMarket.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class StoresController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StoresController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> PendingApproval()
        {
            var pendingStores = await _userManager.Users
                .Where(u => u.UserType == UserType.Seller && !u.IsStoreApproved)
                .Include(u => u.StoreCategories)
                .ThenInclude(sc => sc.Category)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return View(pendingStores);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveStore(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null || user.UserType != UserType.Seller)
            {
                TempData["ErrorMessage"] = "المتجر غير موجود";
                return RedirectToAction("PendingApproval");
            }

            user.IsStoreApproved = true;
            user.StoreApprovedAt = DateTime.Now;
            user.StoreApprovedBy = User.Identity.Name;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"تم اعتماد متجر {user.StoreName} بنجاح";
            }
            else
            {
                TempData["ErrorMessage"] = "حدث خطأ أثناء اعتماد المتجر";
            }

            return RedirectToAction("PendingApproval");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectStore(string id, string reason)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "المتجر غير موجود";
                return RedirectToAction("PendingApproval");
            }

            // يمكنك إضافة منطق لحفظ سبب الرفض أو حذف المستخدم
            user.IsActive = false;
            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = $"تم رفض متجر {user.StoreName}";
            return RedirectToAction("PendingApproval");
        }
    }
}