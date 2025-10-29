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

        // ✅ صفحة جديدة لمراجعة الروابط المعلقة
        public async Task<IActionResult> PendingUrlChanges()
        {
            var storesWithPendingUrls = await _userManager.Users
                .Where(u => u.UserType == UserType.Seller && u.HasPendingUrlChanges)
                .OrderByDescending(u => u.UpdatedAt)
                .ToListAsync();

            return View(storesWithPendingUrls);
        }

        // ✅ الموافقة على الروابط الجديدة
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUrlChanges(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null || user.UserType != UserType.Seller)
            {
                TempData["ErrorMessage"] = "المتجر غير موجود";
                return RedirectToAction("PendingUrlChanges");
            }

            // نقل الروابط من Pending إلى الروابط الفعلية
            user.WebsiteUrl1 = user.PendingWebsiteUrl1;
            user.WebsiteUrl2 = user.PendingWebsiteUrl2;
            user.WebsiteUrl3 = user.PendingWebsiteUrl3;

            // إعادة تعيين الحقول المعلقة
            user.PendingWebsiteUrl1 = null;
            user.PendingWebsiteUrl2 = null;
            user.PendingWebsiteUrl3 = null;
            user.HasPendingUrlChanges = false;
            user.UrlsLastApprovedAt = DateTime.Now;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                // إرسال إشعار بالموافقة
                await NotifyUrlApprovalAsync(user);

                TempData["SuccessMessage"] = $"تم اعتماد الروابط الجديدة لمتجر {user.StoreName}";
            }
            else
            {
                TempData["ErrorMessage"] = "حدث خطأ أثناء اعتماد الروابط";
            }

            return RedirectToAction("PendingUrlChanges");
        }

        // ✅ رفض الروابط الجديدة
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectUrlChanges(string id, string reason)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "المتجر غير موجود";
                return RedirectToAction("PendingUrlChanges");
            }

            // حذف الروابط المعلقة
            user.PendingWebsiteUrl1 = null;
            user.PendingWebsiteUrl2 = null;
            user.PendingWebsiteUrl3 = null;
            user.HasPendingUrlChanges = false;

            await _userManager.UpdateAsync(user);

            // إرسال إشعار بالرفض
            await NotifyUrlRejectionAsync(user, reason);

            TempData["SuccessMessage"] = $"تم رفض الروابط الجديدة لمتجر {user.StoreName}";
            return RedirectToAction("PendingUrlChanges");
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

            await NotifyStoreRejectionAsync(user, reason);

            user.IsActive = false;
            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = $"تم رفض متجر {user.StoreName}";
            return RedirectToAction("PendingApproval");
        }

        // ✅ إرسال إشعار الموافقة على الروابط
        private async Task NotifyUrlApprovalAsync(ApplicationUser store)
        {
            try
            {
                if (!string.IsNullOrEmpty(store.PhoneNumber))
                {
                    var message = $"مرحباً {store.StoreName}!\n\n" +
                                 $"تم اعتماد تحديثات الروابط الخاصة بمتجرك ✅\n\n" +
                                 $"الروابط الجديدة:\n";

                    if (!string.IsNullOrEmpty(store.WebsiteUrl1))
                        message += $"• {store.WebsiteUrl1}\n";
                    if (!string.IsNullOrEmpty(store.WebsiteUrl2))
                        message += $"• {store.WebsiteUrl2}\n";
                    if (!string.IsNullOrEmpty(store.WebsiteUrl3))
                        message += $"• {store.WebsiteUrl3}\n";

                    message += "\nشكراً لك - السوق العكسي";

                    var whatsAppRequest = new WhatsAppMessageRequest
                    {
                        recipient = store.PhoneNumber,
                        message = message
                    };

                    var result = await _whatsAppService.SendMessageAsync(whatsAppRequest);

                    if (result.Success)
                    {
                        _logger.LogInformation("✅ تم إرسال إشعار الموافقة على الروابط إلى {PhoneNumber}",
                            store.PhoneNumber);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إرسال إشعار الموافقة على الروابط");
            }
        }

        // ✅ إرسال إشعار رفض الروابط
        private async Task NotifyUrlRejectionAsync(ApplicationUser store, string reason)
        {
            try
            {
                if (!string.IsNullOrEmpty(store.PhoneNumber))
                {
                    var message = $"مرحباً {store.StoreName}!\n\n" +
                                 $"نأسف لإبلاغك بأن الروابط الجديدة لم تتم الموافقة عليها.\n\n";

                    if (!string.IsNullOrEmpty(reason))
                    {
                        message += $"السبب: {reason}\n\n";
                    }

                    message += "يمكنك إعادة المحاولة بروابط أخرى.\n\n" +
                              "شكراً لتفهمك - السوق العكسي";

                    var whatsAppRequest = new WhatsAppMessageRequest
                    {
                        recipient = store.PhoneNumber,
                        message = message
                    };

                    await _whatsAppService.SendMessageAsync(whatsAppRequest);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إرسال إشعار رفض الروابط");
            }
        }

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