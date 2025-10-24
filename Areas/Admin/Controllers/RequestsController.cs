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
                    .Include(r => r.SubCategory1)
                    .Include(r => r.SubCategory2)
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

                    // ✅ إرسال إشعار للمستخدم بالموافقة
                    await NotifyUserAboutApprovalAsync(request);

                    // ✅ إرسال إشعار للمتاجر المتخصصة - بعد الموافقة فقط
                    await NotifyStoresAboutApprovedRequestAsync(request);
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
                    var messageText = $"مرحبا {request.User.FirstName}!\n\n" +
                                     $"تم الموافقة على طلبك: {request.Title}\n\n" +
                                     $"سيتم اشعار المتاجر المتخصصة وستبدا بتلقي العروض قريبا.\n\n" +
                                     $"شكرا لاستخدامك السوق العكسي";

                    _logger.LogInformation("📤 إرسال رسالة موافقة للمشتري {Phone}:\n{Message}",
                        request.User.PhoneNumber, messageText);

                    var whatsAppRequest = new WhatsAppMessageRequest
                    {
                        recipient = request.User.PhoneNumber,
                        message = messageText,
                        type = "whatsapp",
                        lang = "ar"
                    };

                    var result = await _whatsAppService.SendMessageAsync(whatsAppRequest);

                    if (result.Success)
                    {
                        _logger.LogInformation("✅ تم إرسال إشعار الموافقة بنجاح إلى {PhoneNumber}",
                            request.User.PhoneNumber);
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
                    var messageText = $"مرحبا {request.User.FirstName}!\n\n" +
                                     $"ناسف لابلاغك بان طلبك: {request.Title}\n" +
                                     $"لم تتم الموافقة عليه.\n\n";

                    if (!string.IsNullOrEmpty(request.AdminNotes))
                    {
                        messageText += $"السبب: {request.AdminNotes}\n\n";
                    }

                    messageText += "يمكنك اضافة طلب جديد في اي وقت.\n\n" +
                              "شكرا لتفهمك - السوق العكسي";

                    _logger.LogInformation("📤 إرسال رسالة رفض للمشتري {Phone}:\n{Message}",
                        request.User.PhoneNumber, messageText);

                    var whatsAppRequest = new WhatsAppMessageRequest
                    {
                        recipient = request.User.PhoneNumber,
                        message = messageText,
                        type = "whatsapp",
                        lang = "ar"
                    };

                    var result = await _whatsAppService.SendMessageAsync(whatsAppRequest);

                    if (result.Success)
                    {
                        _logger.LogInformation("✅ تم إرسال إشعار الرفض بنجاح إلى {PhoneNumber}",
                            request.User.PhoneNumber);
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

        // ✅ إرسال إشعار للمتاجر المتخصصة بعد موافقة الأدمن - مصحح ومحسّن
        private async Task NotifyStoresAboutApprovedRequestAsync(Request request)
        {
            try
            {
                _logger.LogInformation("🔍 البحث عن المتاجر المتخصصة للطلب المعتمد #{RequestId}", request.Id);

                // البحث عن المتاجر المتخصصة بنفس الفئة
                var relevantStoresQuery = _dbContext.StoreCategories
                    .Include(sc => sc.User)
                    .Where(sc =>
                        sc.User.UserType == UserType.Seller &&
                        sc.User.IsActive &&
                        sc.User.IsStoreApproved &&
                        !string.IsNullOrEmpty(sc.User.PhoneNumber))
                    .AsQueryable();

                // تطبيق الفلتر بناءً على الفئات
                if (request.SubCategory2Id.HasValue)
                {
                    relevantStoresQuery = relevantStoresQuery.Where(sc =>
                        sc.SubCategory2Id == request.SubCategory2Id);
                }
                else if (request.SubCategory1Id.HasValue)
                {
                    relevantStoresQuery = relevantStoresQuery.Where(sc =>
                        sc.SubCategory1Id == request.SubCategory1Id ||
                        sc.SubCategory2Id.HasValue);
                }
                else
                {
                    relevantStoresQuery = relevantStoresQuery.Where(sc =>
                        sc.CategoryId == request.CategoryId);
                }

                var relevantStores = await relevantStoresQuery
                    .Select(sc => sc.User)
                    .Distinct()
                    .ToListAsync();

                _logger.LogInformation("🔍 تم العثور على {Count} متجر متخصص للطلب المعتمد #{RequestId}",
                    relevantStores.Count, request.Id);

                if (!relevantStores.Any())
                {
                    _logger.LogInformation("ℹ️ لا توجد متاجر متخصصة لهذا الطلب");
                    return;
                }

                // إعداد مسار الفئات
                var categoryPath = request.Category?.Name ?? "غير محدد";
                if (request.SubCategory1 != null)
                {
                    categoryPath += $" > {request.SubCategory1.Name}";
                }
                if (request.SubCategory2 != null)
                {
                    categoryPath += $" > {request.SubCategory2.Name}";
                }

                var successCount = 0;
                var failureCount = 0;

                // إرسال الإشعارات لكل متجر
                foreach (var store in relevantStores)
                {
                    try
                    {
                        // ✅ بناء رسالة مفصلة وواضحة
                        var messageText = $"طلب جديد معتمد في تخصصك!\n\n" +
                                         $"مرحبا {store.StoreName ?? store.FirstName}!\n\n" +
                                         $"عنوان الطلب: {request.Title}\n\n" +
                                         $"الفئة: {categoryPath}\n\n" +
                                         $"الموقع: {request.City} - {request.District}\n\n" +
                                         $"التفاصيل:\n{request.Description}\n\n" +
                                         $"المشتري: {request.User?.FirstName} {request.User?.LastName}\n" +
                                         $"للتواصل: {request.User?.PhoneNumber}\n\n" +
                                         $"تاريخ الطلب: {request.CreatedAt:yyyy-MM-dd}\n\n" +
                                         $"للمشاهدة الكاملة، تفضل بزيارة موقعنا\n\n" +
                                         $"السوق العكسي";

                        // ✅ تسجيل الرسالة للتأكد من محتواها
                        _logger.LogInformation("📤 إرسال رسالة للمتجر {StoreName} - {Phone}:\n{Message}",
                            store.StoreName, store.PhoneNumber, messageText);

                        var whatsAppRequest = new WhatsAppMessageRequest
                        {
                            recipient = store.PhoneNumber,
                            message = messageText,
                            type = "whatsapp",
                            lang = "ar"
                        };

                        var result = await _whatsAppService.SendMessageAsync(whatsAppRequest);

                        if (result.Success)
                        {
                            successCount++;
                            _logger.LogInformation("✅ تم إرسال إشعار بنجاح إلى المتجر {StoreName} - {PhoneNumber}",
                                store.StoreName, store.PhoneNumber);
                        }
                        else
                        {
                            failureCount++;
                            _logger.LogError("❌ فشل إرسال إشعار إلى المتجر {StoreName} - {PhoneNumber}: {Error}",
                                store.StoreName, store.PhoneNumber, result.Message);
                        }

                        // تأخير قصير بين الرسائل لتجنب Rate Limiting
                        await Task.Delay(500);
                    }
                    catch (Exception storeEx)
                    {
                        failureCount++;
                        _logger.LogError(storeEx, "❌ خطأ في إرسال إشعار للمتجر {StoreName}", store.StoreName);
                    }
                }

                _logger.LogInformation("✅ تم الانتهاء من إرسال الإشعارات: {SuccessCount} نجحت، {FailureCount} فشلت",
                    successCount, failureCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطأ عام في إرسال إشعارات المتاجر للطلب #{RequestId}", request.Id);
            }
        }
    }
}