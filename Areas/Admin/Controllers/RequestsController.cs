using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Models;
using ReverseMarket.Models.Identity;
using ReverseMarket.Areas.Admin.Models;
using ReverseMarket.CustomWhatsappService;

namespace ReverseMarket.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class RequestsController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<RequestsController> _logger;
        private readonly WhatsAppService _whatsAppService;

        public RequestsController(
            ApplicationDbContext context,
            ILogger<RequestsController> logger,
            WhatsAppService whatsAppService)
        {
            _dbContext = context;
            _logger = logger;
            _whatsAppService = whatsAppService;
        }

        public async Task<IActionResult> Index(RequestStatus? status = null, int page = 1)
        {
            var pageSize = 20;

            var query = _dbContext.Requests
                .Include(r => r.User)
                .Include(r => r.Category)
                .Include(r => r.SubCategory1)
                .Include(r => r.SubCategory2)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            var totalRequests = await query.CountAsync();
            var requests = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new AdminRequestsViewModel
            {
                Requests = requests,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalRequests / pageSize),
                StatusFilter = status
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var request = await _dbContext.Requests
                .Include(r => r.User)
                .Include(r => r.Category)
                .Include(r => r.SubCategory1)
                .Include(r => r.SubCategory2)
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, int status, string? adminNotes = null)
        {
            try
            {
                var request = await _dbContext.Requests
                    .Include(r => r.User)
                    .Include(r => r.Category)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (request == null)
                {
                    TempData["ErrorMessage"] = "الطلب غير موجود";
                    return RedirectToAction("Index");
                }

                if (!Enum.IsDefined(typeof(RequestStatus), status))
                {
                    TempData["ErrorMessage"] = "حالة الطلب غير صحيحة";
                    return RedirectToAction("Details", new { id });
                }

                var requestStatus = (RequestStatus)status;
                request.Status = requestStatus;
                request.AdminNotes = adminNotes;

                if (requestStatus == RequestStatus.Approved)
                {
                    request.ApprovedAt = DateTime.Now;

                    // إرسال إشعار للمستخدم
                    await NotifyUserAboutApprovalAsync(request);

                    // إرسال إشعار للمتاجر المتخصصة
                    await NotifyStoresAboutNewRequestAsync(request);
                }
                else if (requestStatus == RequestStatus.Rejected)
                {
                    // إرسال إشعار بالرفض
                    await NotifyUserAboutRejectionAsync(request);
                }

                await _dbContext.SaveChangesAsync();

                var statusText = requestStatus switch
                {
                    RequestStatus.Approved => "تم اعتماد",
                    RequestStatus.Rejected => "تم رفض",
                    RequestStatus.Postponed => "تم تأجيل",
                    _ => "تم تحديث"
                };

                TempData["SuccessMessage"] = $"{statusText} الطلب بنجاح";

                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحديث حالة الطلب: {RequestId}", id);
                TempData["ErrorMessage"] = "حدث خطأ أثناء تحديث حالة الطلب";

                return RedirectToAction("Details", new { id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var request = await _dbContext.Requests
                .Include(r => r.Category)
                .Include(r => r.SubCategory1)
                .Include(r => r.SubCategory2)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            ViewBag.Categories = await _dbContext.Categories.Where(c => c.IsActive).ToListAsync();
            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Request model)
        {
            try
            {
                var request = await _dbContext.Requests.FindAsync(model.Id);
                if (request == null)
                {
                    return NotFound();
                }

                request.Title = model.Title;
                request.Description = model.Description;
                request.CategoryId = model.CategoryId;
                request.SubCategory1Id = model.SubCategory1Id;
                request.SubCategory2Id = model.SubCategory2Id;
                request.City = model.City;
                request.District = model.District;
                request.Location = model.Location;
                request.AdminNotes = model.AdminNotes;

                _dbContext.Update(request);
                await _dbContext.SaveChangesAsync();

                TempData["SuccessMessage"] = "تم تحديث الطلب بنجاح";
                return RedirectToAction("Details", new { id = model.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحديث الطلب: {RequestId}", model.Id);
                TempData["ErrorMessage"] = "حدث خطأ أثناء تحديث الطلب";

                ViewBag.Categories = await _dbContext.Categories.Where(c => c.IsActive).ToListAsync();
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var request = await _dbContext.Requests
                    .Include(r => r.Images)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (request == null)
                {
                    TempData["ErrorMessage"] = "الطلب غير موجود";
                    return RedirectToAction("Index");
                }

                if (request.Images != null && request.Images.Any())
                {
                    _dbContext.RequestImages.RemoveRange(request.Images);
                }

                _dbContext.Requests.Remove(request);
                await _dbContext.SaveChangesAsync();

                TempData["SuccessMessage"] = "تم حذف الطلب بنجاح";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حذف الطلب: {RequestId}", id);
                TempData["ErrorMessage"] = "حدث خطأ أثناء حذف الطلب";
                return RedirectToAction("Details", new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleRequestStatus(int id)
        {
            try
            {
                var request = await _dbContext.Requests.FindAsync(id);
                if (request == null)
                {
                    TempData["ErrorMessage"] = "الطلب غير موجود";
                    return RedirectToAction("Index");
                }

                if (request.Status == RequestStatus.Approved)
                {
                    request.Status = RequestStatus.Postponed;
                    TempData["SuccessMessage"] = "تم إيقاف الطلب";
                }
                else if (request.Status == RequestStatus.Postponed)
                {
                    request.Status = RequestStatus.Approved;
                    request.ApprovedAt = DateTime.Now;
                    TempData["SuccessMessage"] = "تم تفعيل الطلب";
                }

                await _dbContext.SaveChangesAsync();
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تبديل حالة الطلب: {RequestId}", id);
                TempData["ErrorMessage"] = "حدث خطأ أثناء تبديل حالة الطلب";
                return RedirectToAction("Details", new { id });
            }
        }

        // ✅ إرسال إشعار للمستخدم بالموافقة على الطلب
        private async Task NotifyUserAboutApprovalAsync(Request request)
        {
            try
            {
                if (request.User != null && !string.IsNullOrEmpty(request.User.PhoneNumber))
                {
                    var message = $"مرحباً {request.User.FirstName}!\n\n" +
                                 $"تم الموافقة على طلبك: {request.Title}\n\n" +
                                 $"سيتم إشعار المتاجر المتخصصة وستبدأ بتلقي العروض قريباً.\n\n" +
                                 $"شكراً لاستخدامك السوق العكسي";

                    var whatsAppRequest = new WhatsAppMessageRequest
                    {
                        recipient = request.User.PhoneNumber,
                        message = message
                    };

                    var result = await _whatsAppService.SendMessageAsync(whatsAppRequest);

                    if (result.Success)
                    {
                        _logger.LogInformation("✅ تم إرسال إشعار الموافقة بنجاح إلى {PhoneNumber}", request.User.PhoneNumber);
                    }
                    else
                    {
                        _logger.LogError("❌ فشل إرسال إشعار الموافقة إلى {PhoneNumber}: {Error}",
                            request.User.PhoneNumber, result.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إرسال إشعار الموافقة");
            }
        }

        // ✅ إرسال إشعار للمستخدم برفض الطلب
        private async Task NotifyUserAboutRejectionAsync(Request request)
        {
            try
            {
                if (request.User != null && !string.IsNullOrEmpty(request.User.PhoneNumber))
                {
                    var message = $"مرحباً {request.User.FirstName}!\n\n" +
                                 $"نأسف لإبلاغك بأن طلبك: {request.Title}\n" +
                                 $"لم تتم الموافقة عليه.\n\n";

                    if (!string.IsNullOrEmpty(request.AdminNotes))
                    {
                        message += $"السبب: {request.AdminNotes}\n\n";
                    }

                    message += "يمكنك إضافة طلب جديد في أي وقت.\n\n" +
                              "شكراً لتفهمك - السوق العكسي";

                    var whatsAppRequest = new WhatsAppMessageRequest
                    {
                        recipient = request.User.PhoneNumber,
                        message = message
                    };

                    var result = await _whatsAppService.SendMessageAsync(whatsAppRequest);

                    if (result.Success)
                    {
                        _logger.LogInformation("✅ تم إرسال إشعار الرفض بنجاح إلى {PhoneNumber}", request.User.PhoneNumber);
                    }
                    else
                    {
                        _logger.LogError("❌ فشل إرسال إشعار الرفض إلى {PhoneNumber}: {Error}",
                            request.User.PhoneNumber, result.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إرسال إشعار الرفض");
            }
        }

        // ✅ إرسال إشعار للمتاجر المتخصصة عن طلب جديد
        private async Task NotifyStoresAboutNewRequestAsync(Request request)
        {
            try
            {
                // البحث عن المتاجر المتخصصة بنفس الفئة
                var relevantStores = await _dbContext.StoreCategories
                    .Include(sc => sc.User)
                    .Where(sc =>
                        (sc.CategoryId == request.CategoryId ||
                         sc.SubCategory1Id == request.SubCategory1Id ||
                         sc.SubCategory2Id == request.SubCategory2Id) &&
                        sc.User.UserType == UserType.Seller &&
                        sc.User.IsActive &&
                        sc.User.IsStoreApproved)
                    .Select(sc => sc.User)
                    .Distinct()
                    .ToListAsync();

                _logger.LogInformation("🔍 تم العثور على {Count} متجر متخصص للطلب {RequestId}",
                    relevantStores.Count, request.Id);

                foreach (var store in relevantStores)
                {
                    if (!string.IsNullOrEmpty(store.PhoneNumber))
                    {
                        var message = $"🔔 طلب جديد في تخصصك!\n\n" +
                                     $"مرحباً {store.StoreName ?? store.FirstName}!\n\n" +
                                     $"📝 الطلب: {request.Title}\n" +
                                     $"📂 الفئة: {request.Category?.Name}\n" +
                                     $"📍 الموقع: {request.City} - {request.District}\n\n" +
                                     $"للمشاهدة والتواصل مع العميل، تفضل بزيارة موقعنا\n\n" +
                                     $"السوق العكسي";

                        var whatsAppRequest = new WhatsAppMessageRequest
                        {
                            recipient = store.PhoneNumber,
                            message = message
                        };

                        var result = await _whatsAppService.SendMessageAsync(whatsAppRequest);

                        if (result.Success)
                        {
                            _logger.LogInformation("✅ تم إرسال إشعار بنجاح إلى المتجر {StoreName} - {PhoneNumber}",
                                store.StoreName, store.PhoneNumber);
                        }
                        else
                        {
                            _logger.LogError("❌ فشل إرسال إشعار إلى المتجر {StoreName} - {PhoneNumber}: {Error}",
                                store.StoreName, store.PhoneNumber, result.Message);
                        }

                        // تأخير قصير بين الرسائل لتجنب Rate Limiting
                        await Task.Delay(500);
                    }
                }

                _logger.LogInformation("✅ تم الانتهاء من إرسال الإشعارات لـ {Count} متجر", relevantStores.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إرسال إشعارات المتاجر");
            }
        }
    }
}