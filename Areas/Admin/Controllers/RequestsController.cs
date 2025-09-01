using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Models;
using ReverseMarket.Models.Identity; // إضافة هذا
using ReverseMarket.Areas.Admin.Models;

namespace ReverseMarket.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class RequestsController : Controller
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<RequestsController> _logger; // إضافة Logger

        public RequestsController(ApplicationDbContext context, ILogger<RequestsController> logger)
        {
            _dbContext = context;
            _logger = logger; // تهيئة Logger
        }

        // باقي الكود كما هو...

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
                var request = await _dbContext.Requests.FindAsync(id);
                if (request == null)
                {
                    TempData["ErrorMessage"] = "الطلب غير موجود";
                    return RedirectToAction("Index");
                }

                // التحقق من صحة الحالة
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

                    // إشعار المستخدم بالموافقة
                    await NotifyUserAboutApprovalAsync(request);

                    // إشعار المتاجر ذات الصلة
                    await NotifyStoresAboutNewRequestAsync(request);
                }

                await _dbContext.SaveChangesAsync();

                // رسالة نجاح
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
                // تسجيل الخطأ
                _logger.LogError(ex, "خطأ في تحديث حالة الطلب: {RequestId}", id);
                TempData["ErrorMessage"] = "حدث خطأ أثناء تحديث حالة الطلب";

                return RedirectToAction("Details", new { id });
            }
        }

        private async Task NotifyUserAboutApprovalAsync(Request request)
        {
            try
            {
                var user = await _dbContext.Users.FindAsync(request.UserId);
                if (user != null && !string.IsNullOrEmpty(user.PhoneNumber))
                {
                    var message = $"مرحباً {user.FirstName}!\n\n" +
                                 $"تم الموافقة على طلبك: {request.Title}\n\n" +
                                 $"سيتم إشعار المتاجر المتخصصة وستبدأ بتلقي العروض قريباً.\n\n" +
                                 $"شكراً لاستخدامك السوق العكسي";

                    // إرسال إشعار واتساب
                    _logger.LogInformation("WhatsApp إلى {PhoneNumber}: {Message}", user.PhoneNumber, message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إرسال إشعار الموافقة");
            }
        }

        private async Task NotifyStoresAboutNewRequestAsync(Request request)
        {
            try
            {
                var relevantStores = await _dbContext.StoreCategories
                    .Include(sc => sc.User)
                    .Where(sc => sc.CategoryId == request.CategoryId ||
                               sc.SubCategory1Id == request.SubCategory1Id ||
                               sc.SubCategory2Id == request.SubCategory2Id)
                    .Select(sc => sc.User)
                    .Where(u => u.UserType == UserType.Seller)
                    .Distinct()
                    .ToListAsync();

                foreach (var store in relevantStores)
                {
                    if (!string.IsNullOrEmpty(store.PhoneNumber))
                    {
                        var message = $"مرحباً {store.StoreName ?? store.FirstName}!\n\n" +
                                     $"طلب جديد في متجركم: {request.Title}\n" +
                                     $"الموقع: {request.City} - {request.District}\n\n" +
                                     $"للمشاهدة والتواصل مع العميل، تفضل بزيارة موقعنا\n\n" +
                                     $"السوق العكسي";

                        // إرسال إشعار واتساب
                        _logger.LogInformation("WhatsApp إلى {PhoneNumber}: {Message}", store.PhoneNumber, message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إرسال إشعارات المتاجر");
            }
        }
    }
}