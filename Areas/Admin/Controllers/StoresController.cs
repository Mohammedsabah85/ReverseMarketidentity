using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Models.Identity;
using ReverseMarket.CustomWhatsappService;

namespace ReverseMarket.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class StoresController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly WhatsAppService _whatsAppService;
        private readonly ILogger<StoresController> _logger;

        public StoresController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            WhatsAppService whatsAppService,
            ILogger<StoresController> logger)
        {
            _context = context;
            _userManager = userManager;
            _whatsAppService = whatsAppService;
            _logger = logger;
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
                // ✅ إرسال إشعار الموافقة على المتجر
                await NotifyStoreApprovalAsync(user);

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

            // ✅ إرسال إشعار الرفض
            await NotifyStoreRejectionAsync(user, reason);

            user.IsActive = false;
            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = $"تم رفض متجر {user.StoreName}";
            return RedirectToAction("PendingApproval");
        }

        // ✅ إرسال إشعار الموافقة على المتجر
        private async Task NotifyStoreApprovalAsync(ApplicationUser store)
        {
            try
            {
                if (!string.IsNullOrEmpty(store.PhoneNumber))
                {
                    var message = $"🎉 تهانينا {store.StoreName}!\n\n" +
                                 $"تم اعتماد متجرك في السوق العكسي بنجاح! ✅\n\n" +
                                 $"يمكنك الآن:\n" +
                                 $"• استقبال الطلبات الجديدة\n" +
                                 $"• التواصل مع العملاء\n" +
                                 $"• تقديم عروضك الخاصة\n\n" +
                                 $"نتمنى لك تجربة موفقة معنا!\n\n" +
                                 $"السوق العكسي";

                    var whatsAppRequest = new WhatsAppMessageRequest
                    {
                        recipient = store.PhoneNumber,
                        message = message
                    };

                    var result = await _whatsAppService.SendMessageAsync(whatsAppRequest);

                    if (result.Success)
                    {
                        _logger.LogInformation("✅ تم إرسال إشعار الموافقة على المتجر بنجاح إلى {PhoneNumber}",
                            store.PhoneNumber);
                    }
                    else
                    {
                        _logger.LogError("❌ فشل إرسال إشعار الموافقة على المتجر إلى {PhoneNumber}: {Error}",
                            store.PhoneNumber, result.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إرسال إشعار الموافقة على المتجر");
            }
        }

        // ✅ إرسال إشعار رفض المتجر
        private async Task NotifyStoreRejectionAsync(ApplicationUser store, string reason)
        {
            try
            {
                if (!string.IsNullOrEmpty(store.PhoneNumber))
                {
                    var message = $"مرحباً {store.StoreName}!\n\n" +
                                 $"نأسف لإبلاغك بأن طلب اعتماد متجرك لم تتم الموافقة عليه.\n\n";

                    if (!string.IsNullOrEmpty(reason))
                    {
                        message += $"السبب: {reason}\n\n";
                    }

                    message += "يمكنك التواصل معنا لمزيد من التفاصيل.\n\n" +
                              "شكراً لتفهمك - السوق العكسي";

                    var whatsAppRequest = new WhatsAppMessageRequest
                    {
                        recipient = store.PhoneNumber,
                        message = message
                    };

                    var result = await _whatsAppService.SendMessageAsync(whatsAppRequest);

                    if (result.Success)
                    {
                        _logger.LogInformation("✅ تم إرسال إشعار رفض المتجر بنجاح إلى {PhoneNumber}",
                            store.PhoneNumber);
                    }
                    else
                    {
                        _logger.LogError("❌ فشل إرسال إشعار رفض المتجر إلى {PhoneNumber}: {Error}",
                            store.PhoneNumber, result.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إرسال إشعار رفض المتجر");
            }
        }
    }
}