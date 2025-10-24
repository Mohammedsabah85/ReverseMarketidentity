using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Models;
using ReverseMarket.Services;
using Microsoft.AspNetCore.Identity;
using ReverseMarket.Models.Identity;
using ReverseMarket.CustomWhatsappService;

namespace ReverseMarket.Controllers
{
    public class RequestsController : BaseController
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IWhatsAppService _whatsAppService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RequestsController> _logger;
        private readonly WhatsAppService _customWhatsAppService;

        public RequestsController(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            IWhatsAppService whatsAppService,
            UserManager<ApplicationUser> userManager,
            ILogger<RequestsController> logger,
            WhatsAppService customWhatsAppService)
            : base(context)
        {
            _webHostEnvironment = webHostEnvironment;
            _whatsAppService = whatsAppService;
            _userManager = userManager;
            _logger = logger;
            _customWhatsAppService = customWhatsAppService;
        }

        public async Task<IActionResult> Index(string search, int? categoryId, int page = 1)
        {
            var pageSize = 12;

            var query = _context.Requests
                .Where(r => r.Status == RequestStatus.Approved)
                .Include(r => r.Category)
                .Include(r => r.SubCategory1)
                .Include(r => r.SubCategory2)
                .Include(r => r.Images)
                .Include(r => r.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r => r.Title.Contains(search) || r.Description.Contains(search));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(r => r.CategoryId == categoryId.Value);
            }

            var totalRequests = await query.CountAsync();
            var requests = await query
                .OrderByDescending(r => r.ApprovedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new RequestsViewModel
            {
                Requests = requests,
                Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync(),
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalRequests / pageSize),
                Search = search,
                SelectedCategoryId = categoryId
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var request = await _context.Requests
                .Include(r => r.Category)
                .Include(r => r.SubCategory1)
                .Include(r => r.SubCategory2)
                .Include(r => r.Images)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id && r.Status == RequestStatus.Approved);

            if (request == null)
            {
                return NotFound();
            }

            return View(request);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Create(CreateRequestViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["ErrorMessage"] = "جلسة المستخدم منتهية الصلاحية";
                    return RedirectToAction("Login", "Account");
                }

                try
                {
                    var request = new Request
                    {
                        Title = model.Title,
                        Description = model.Description,
                        CategoryId = model.CategoryId,
                        SubCategory1Id = model.SubCategory1Id,
                        SubCategory2Id = model.SubCategory2Id,
                        City = model.City,
                        District = model.District,
                        Location = model.Location,
                        UserId = userId,
                        Status = RequestStatus.Pending,
                        CreatedAt = DateTime.Now
                    };

                    _context.Requests.Add(request);
                    await _context.SaveChangesAsync();

                    // معالجة رفع الصور
                    if (model.Images != null && model.Images.Any())
                    {
                        await SaveRequestImagesAsync(request.Id, model.Images);
                    }

                    // ✅ إرسال إشعار للإدارة عن الطلب الجديد
                    await NotifyAdminAboutNewRequestAsync(request);

                    // ✅ إرسال إشعارات للمتاجر المتخصصة
                    await NotifyRelevantStoresAboutNewRequestAsync(request);

                    TempData["SuccessMessage"] = "تم إرسال طلبك بنجاح! سيتم مراجعته والموافقة عليه في أقرب وقت.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ في إنشاء الطلب");
                    TempData["ErrorMessage"] = "حدث خطأ أثناء إرسال الطلب. يرجى المحاولة مرة أخرى.";
                }
            }

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            return View(model);
        }

        private async Task SaveRequestImagesAsync(int requestId, List<IFormFile> images)
        {
            try
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "requests");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var maxFileSize = 5 * 1024 * 1024; // 5 MB

                foreach (var image in images.Take(3))
                {
                    if (image?.Length > 0)
                    {
                        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(extension))
                        {
                            continue;
                        }

                        if (image.Length > maxFileSize)
                        {
                            continue;
                        }

                        var fileName = $"{Guid.NewGuid()}{extension}";
                        var filePath = Path.Combine(uploadsFolder, fileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await image.CopyToAsync(fileStream);
                        }

                        var requestImage = new RequestImage
                        {
                            RequestId = requestId,
                            ImagePath = $"/uploads/requests/{fileName}",
                            CreatedAt = DateTime.Now
                        };

                        _context.RequestImages.Add(requestImage);
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حفظ الصور");
            }
        }

        // ✅ إرسال إشعار للإدارة عن طلب جديد
        private async Task NotifyAdminAboutNewRequestAsync(Request request)
        {
            try
            {
                // رقم الإدارة
                var adminPhone = "+9647700227210";

                var user = await _userManager.FindByIdAsync(request.UserId);
                var category = await _context.Categories.FindAsync(request.CategoryId);

                var message = $"📢 طلب جديد يحتاج مراجعة!\n\n" +
                             $"👤 المستخدم: {user?.FirstName} {user?.LastName}\n" +
                             $"📝 العنوان: {request.Title}\n" +
                             $"📂 الفئة: {category?.Name}\n" +
                             $"📍 الموقع: {request.City} - {request.District}\n" +
                             $"🕐 التاريخ: {request.CreatedAt:yyyy-MM-dd HH:mm}\n\n" +
                             $"يرجى مراجعة الطلب واعتماده\n\n" +
                             $"السوق العكسي";

                var whatsAppRequest = new WhatsAppMessageRequest
                {
                    recipient = adminPhone,
                    message = message
                };

                var result = await _customWhatsAppService.SendMessageAsync(whatsAppRequest);

                if (result.Success)
                {
                    _logger.LogInformation("✅ تم إرسال إشعار للإدارة عن طلب جديد #{RequestId}", request.Id);
                }
                else
                {
                    _logger.LogError("❌ فشل إرسال إشعار للإدارة: {Error}", result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إرسال إشعار للإدارة");
            }
        }

        // ✅ إرسال إشعارات للمتاجر المتخصصة عند إنشاء طلب جديد
        private async Task NotifyRelevantStoresAboutNewRequestAsync(Request request)
        {
            try
            {
                _logger.LogInformation("🔍 البحث عن المتاجر المتخصصة للطلب #{RequestId}", request.Id);

                // جلب بيانات الطلب كاملة
                var fullRequest = await _context.Requests
                    .Include(r => r.Category)
                    .Include(r => r.SubCategory1)
                    .Include(r => r.SubCategory2)
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.Id == request.Id);

                if (fullRequest == null)
                {
                    _logger.LogWarning("⚠️ لم يتم العثور على الطلب #{RequestId}", request.Id);
                    return;
                }

                // البحث عن المتاجر المتخصصة بنفس الفئة أو الفئات الفرعية
                var relevantStoresQuery = _context.StoreCategories
                    .Include(sc => sc.User)
                    .Where(sc =>
                        sc.User.UserType == UserType.Seller &&
                        sc.User.IsActive &&
                        sc.User.IsStoreApproved &&
                        !string.IsNullOrEmpty(sc.User.PhoneNumber))
                    .AsQueryable();

                // تطبيق الفلتر بناءً على الفئات
                if (fullRequest.SubCategory2Id.HasValue)
                {
                    // إذا كان هناك فئة فرعية ثانية، ابحث عن المتاجر التي لديها نفس الفئة الفرعية الثانية
                    relevantStoresQuery = relevantStoresQuery.Where(sc =>
                        sc.SubCategory2Id == fullRequest.SubCategory2Id);
                }
                else if (fullRequest.SubCategory1Id.HasValue)
                {
                    // إذا كان هناك فئة فرعية أولى فقط، ابحث عن المتاجر التي لديها نفس الفئة الفرعية الأولى أو الثانية
                    relevantStoresQuery = relevantStoresQuery.Where(sc =>
                        sc.SubCategory1Id == fullRequest.SubCategory1Id ||
                        sc.SubCategory2Id == fullRequest.SubCategory2Id);
                }
                else
                {
                    // إذا لم يكن هناك فئات فرعية، ابحث عن المتاجر التي لديها نفس الفئة الرئيسية
                    relevantStoresQuery = relevantStoresQuery.Where(sc =>
                        sc.CategoryId == fullRequest.CategoryId);
                }

                var relevantStores = await relevantStoresQuery
                    .Select(sc => sc.User)
                    .Distinct()
                    .ToListAsync();

                _logger.LogInformation("🔍 تم العثور على {Count} متجر متخصص للطلب #{RequestId}",
                    relevantStores.Count, request.Id);

                if (!relevantStores.Any())
                {
                    _logger.LogInformation("ℹ️ لا توجد متاجر متخصصة لهذا الطلب");
                    return;
                }

                // إعداد رسالة الإشعار
                var categoryPath = fullRequest.Category?.Name ?? "غير محدد";
                if (fullRequest.SubCategory1 != null)
                {
                    categoryPath += $" > {fullRequest.SubCategory1.Name}";
                }
                if (fullRequest.SubCategory2 != null)
                {
                    categoryPath += $" > {fullRequest.SubCategory2.Name}";
                }

                // إرسال الإشعارات لكل متجر
                var successCount = 0;
                var failureCount = 0;

                foreach (var store in relevantStores)
                {
                    try
                    {
                        var message = $"🔔 طلب جديد في تخصصك!\n\n" +
                                     $"مرحباً {store.StoreName ?? store.FirstName}!\n\n" +
                                     $"📝 الطلب: {fullRequest.Title}\n" +
                                     $"📂 الفئة: {categoryPath}\n" +
                                     $"📍 الموقع: {fullRequest.City} - {fullRequest.District}\n" +
                                     $"📋 الوصف: {(fullRequest.Description.Length > 100 ? fullRequest.Description.Substring(0, 100) + "..." : fullRequest.Description)}\n\n" +
                                     $"⚠️ ملاحظة: الطلب بانتظار موافقة الإدارة\n" +
                                     $"سيتم إشعارك مرة أخرى عند اعتماده\n\n" +
                                     $"للمشاهدة والتواصل مع العميل، تفضل بزيارة موقعنا\n\n" +
                                     $"السوق العكسي";

                        var whatsAppRequest = new WhatsAppMessageRequest
                        {
                            recipient = store.PhoneNumber,
                            message = message
                        };

                        var result = await _customWhatsAppService.SendMessageAsync(whatsAppRequest);

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

        [HttpGet]
        public async Task<IActionResult> GetSubCategories1(int categoryId)
        {
            var subCategories = await _context.SubCategories1
                .Where(sc => sc.CategoryId == categoryId && sc.IsActive)
                .Select(sc => new { id = sc.Id, name = sc.Name })
                .ToListAsync();

            return Json(subCategories);
        }

        [HttpGet]
        public async Task<IActionResult> GetSubCategories2(int subCategory1Id)
        {
            var subCategories = await _context.SubCategories2
                .Where(sc => sc.SubCategory1Id == subCategory1Id && sc.IsActive)
                .Select(sc => new { id = sc.Id, name = sc.Name })
                .ToListAsync();

            return Json(subCategories);
        }
    }
}