using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Models;
using ReverseMarket.Areas.Admin.Models;

namespace ReverseMarket.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string search, UserType? userType, bool? isActive, int page = 1)
        {
            var pageSize = 20;

            var query = _context.Users.AsQueryable();

            // تطبيق الفلاتر
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.FirstName.Contains(search) ||
                                       u.LastName.Contains(search) ||
                                       u.PhoneNumber.Contains(search) ||
                                       u.Email.Contains(search) ||
                                       u.StoreName.Contains(search));
            }

            if (userType.HasValue)
            {
                query = query.Where(u => u.UserType == userType.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            var totalUsers = await query.CountAsync();

            var users = await query
                .Include(u => u.StoreCategories)
                .ThenInclude(sc => sc.Category)
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = new AdminUsersViewModel
            {
                Users = users,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)totalUsers / pageSize),
                Search = search,
                UserTypeFilter = userType,
                IsActiveFilter = isActive,
                TotalUsers = totalUsers,
                ActiveUsers = await _context.Users.CountAsync(u => u.IsActive),
                InactiveUsers = await _context.Users.CountAsync(u => !u.IsActive),
                BuyersCount = await _context.Users.CountAsync(u => u.UserType == UserType.Buyer),
                SellersCount = await _context.Users.CountAsync(u => u.UserType == UserType.Seller)
            };

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.Users
                .Include(u => u.StoreCategories)
                .ThenInclude(sc => sc.Category)
                .Include(u => u.StoreCategories)
                .ThenInclude(sc => sc.SubCategory1)
                .Include(u => u.StoreCategories)
                .ThenInclude(sc => sc.SubCategory2)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // إحصائيات المستخدم
            var userStats = new UserStatistics
            {
                TotalRequests = await _context.Requests.CountAsync(r => r.UserId == id),
                ApprovedRequests = await _context.Requests.CountAsync(r => r.UserId == id && r.Status == RequestStatus.Approved),
                PendingRequests = await _context.Requests.CountAsync(r => r.UserId == id && r.Status == RequestStatus.Pending),
                RejectedRequests = await _context.Requests.CountAsync(r => r.UserId == id && r.Status == RequestStatus.Rejected)
            };

            ViewBag.UserStats = userStats;
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "المستخدم غير موجود";
                    return RedirectToAction("Index");
                }

                // منع إيقاف الأدمن
                var adminPhone = "+9647700227210";
                if (user.PhoneNumber == adminPhone)
                {
                    TempData["ErrorMessage"] = "لا يمكن إيقاف حساب الأدمن";
                    return RedirectToAction("Details", new { id });
                }

                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();

                var statusText = user.IsActive ? "تفعيل" : "إيقاف";
                TempData["SuccessMessage"] = $"تم {statusText} المستخدم بنجاح";

                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"خطأ في تغيير حالة المستخدم: {ex.Message}");
                TempData["ErrorMessage"] = "حدث خطأ أثناء تحديث حالة المستخدم";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "المستخدم غير موجود";
                    return RedirectToAction("Index");
                }

                // منع حذف الأدمن
                var adminPhone = "+9647700227210";
                if (user.PhoneNumber == adminPhone)
                {
                    TempData["ErrorMessage"] = "لا يمكن حذف حساب الأدمن";
                    return RedirectToAction("Details", new { id });
                }

                // التحقق من وجود طلبات مرتبطة
                var hasRequests = await _context.Requests.AnyAsync(r => r.UserId == id);
                if (hasRequests)
                {
                    TempData["ErrorMessage"] = "لا يمكن حذف هذا المستخدم لأنه يملك طلبات مرتبطة";
                    return RedirectToAction("Details", new { id });
                }

                // حذف فئات المتجر المرتبطة
                var storeCategories = await _context.StoreCategories
                    .Where(sc => sc.UserId == id)
                    .ToListAsync();

                if (storeCategories.Any())
                {
                    _context.StoreCategories.RemoveRange(storeCategories);
                }

                // حذف المستخدم
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "تم حذف المستخدم بنجاح";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"خطأ في حذف المستخدم: {ex.Message}");
                TempData["ErrorMessage"] = "حدث خطأ أثناء حذف المستخدم";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users
                .Include(u => u.StoreCategories)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var model = new EditUserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                City = user.City,
                District = user.District,
                Location = user.Location,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                UserType = user.UserType,
                StoreName = user.StoreName,
                StoreDescription = user.StoreDescription,
                WebsiteUrl1 = user.WebsiteUrl1,
                WebsiteUrl2 = user.WebsiteUrl2,
                WebsiteUrl3 = user.WebsiteUrl3,
                IsActive = user.IsActive,
                IsPhoneVerified = user.IsPhoneVerified,
                CurrentStoreCategories = user.StoreCategories.Select(sc => sc.CategoryId).ToList()
            };

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _context.Users
                        .Include(u => u.StoreCategories)
                        .FirstOrDefaultAsync(u => u.Id == model.Id);

                    if (user == null)
                    {
                        return NotFound();
                    }

                    // تحديث البيانات الأساسية
                    user.FirstName = model.FirstName;
                    user.LastName = model.LastName;
                    user.Email = model.Email;
                    user.City = model.City;
                    user.District = model.District;
                    user.Location = model.Location;
                    user.DateOfBirth = model.DateOfBirth;
                    user.Gender = model.Gender;
                    user.StoreName = model.StoreName;
                    user.StoreDescription = model.StoreDescription;
                    user.WebsiteUrl1 = model.WebsiteUrl1;
                    user.WebsiteUrl2 = model.WebsiteUrl2;
                    user.WebsiteUrl3 = model.WebsiteUrl3;
                    user.IsActive = model.IsActive;
                    user.IsPhoneVerified = model.IsPhoneVerified;

                    // تحديث فئات المتجر للبائعين
                    if (user.UserType == UserType.Seller && model.StoreCategories?.Any() == true)
                    {
                        // حذف الفئات الحالية
                        _context.StoreCategories.RemoveRange(user.StoreCategories);

                        // إضافة الفئات الجديدة
                        foreach (var categoryId in model.StoreCategories)
                        {
                            user.StoreCategories.Add(new StoreCategory
                            {
                                CategoryId = categoryId,
                                CreatedAt = DateTime.Now
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "تم تحديث بيانات المستخدم بنجاح";
                    return RedirectToAction("Details", new { id = model.Id });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"خطأ في تحديث المستخدم: {ex.Message}");
                    TempData["ErrorMessage"] = "حدث خطأ أثناء تحديث البيانات";
                }
            }

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            return View(model);
        }

        // API للإحصائيات
        [HttpGet]
        public async Task<IActionResult> GetUserStats()
        {
            try
            {
                var stats = new
                {
                    TotalUsers = await _context.Users.CountAsync(),
                    ActiveUsers = await _context.Users.CountAsync(u => u.IsActive),
                    InactiveUsers = await _context.Users.CountAsync(u => !u.IsActive),
                    BuyersCount = await _context.Users.CountAsync(u => u.UserType == UserType.Buyer),
                    SellersCount = await _context.Users.CountAsync(u => u.UserType == UserType.Seller),
                    VerifiedUsers = await _context.Users.CountAsync(u => u.IsPhoneVerified),
                    UnverifiedUsers = await _context.Users.CountAsync(u => !u.IsPhoneVerified),
                    NewUsersThisMonth = await _context.Users.CountAsync(u => u.CreatedAt.Month == DateTime.Now.Month && u.CreatedAt.Year == DateTime.Now.Year)
                };

                return Json(stats);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"خطأ في جلب الإحصائيات: {ex.Message}");
                return BadRequest("حدث خطأ في جلب الإحصائيات");
            }
        }
    }

    // نماذج البيانات المطلوبة
    //public class UserStatistics
    //{
    //    public int TotalRequests { get; set; }
    //    public int ApprovedRequests { get; set; }
    //    public int PendingRequests { get; set; }
    //    public int RejectedRequests { get; set; }
    //}
}