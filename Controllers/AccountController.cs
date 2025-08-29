using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Models;
using ReverseMarket.Services;
using System.ComponentModel.DataAnnotations;

namespace ReverseMarket.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWhatsAppService _whatsAppService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AccountController(ApplicationDbContext context, IWhatsAppService whatsAppService, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _whatsAppService = whatsAppService;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // تحقق من تسجيل الدخول المسبق
            if (HttpContext.Session.GetString("IsLoggedIn") == "true")
            {
                var userType = HttpContext.Session.GetString("UserType");
                if (userType == "Admin")
                {
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }
                return RedirectToAction("Index", "Home");
            }

            var model = new LoginViewModel
            {
                CountryCode = "+964"
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // تنظيف رقم الهاتف
                var cleanPhoneNumber = model.CountryCode + model.PhoneNumber.TrimStart('0');

                // التحقق من الأدمن أولاً
                if (cleanPhoneNumber == "+9647700227210") // رقم الأدمن
                {
                    // إنشاء جلسة أدمن مؤقتة للتحقق من OTP
                    var adminOtp = GenerateOTP();
                    HttpContext.Session.SetString("AdminOTP", adminOtp);
                    HttpContext.Session.SetString("AdminPhone", cleanPhoneNumber);
                    HttpContext.Session.SetString("LoginType", "Admin");

                    // إرسال OTP للأدمن
                    await _whatsAppService.SendOTPAsync(cleanPhoneNumber, adminOtp);

                    TempData["InfoMessage"] = "تم إرسال رمز التحقق للأدمن";
                    return RedirectToAction("VerifyAdminOTP");
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == cleanPhoneNumber);

                if (user != null)
                {
                    // مستخدم موجود ومؤكد - إرسال OTP للدخول مباشرة
                    if (user.IsPhoneVerified)
                    {
                        var otp = GenerateOTP();

                        // حفظ OTP في الجلسة
                        HttpContext.Session.SetString("OTP", otp);
                        HttpContext.Session.SetString("PhoneNumber", cleanPhoneNumber);
                        HttpContext.Session.SetString("LoginType", "ExistingVerifiedUser");
                        HttpContext.Session.SetInt32("UserId", user.Id);

                        // إرسال OTP للدخول عبر WhatsApp
                        await _whatsAppService.SendLoginOTPAsync(cleanPhoneNumber, otp, user.FirstName);

                        TempData["InfoMessage"] = $"مرحباً {user.FirstName}! تم إرسال رمز الدخول إلى واتساب.";
                        return RedirectToAction("VerifyOTP");
                    }
                    else
                    {
                        // مستخدم موجود لكن غير مؤكد - إعادة التحقق
                        var verificationCode = GenerateOTP();

                        HttpContext.Session.SetString("VerificationCode", verificationCode);
                        HttpContext.Session.SetString("PhoneNumber", cleanPhoneNumber);
                        HttpContext.Session.SetString("LoginType", "ExistingUnverifiedUser");
                        HttpContext.Session.SetInt32("UserId", user.Id);

                        await _whatsAppService.SendPhoneVerificationAsync(cleanPhoneNumber, verificationCode, true);

                        TempData["WarningMessage"] = "حسابك موجود لكن الهاتف غير مؤكد. يرجى تأكيد رقم الهاتف.";
                        return RedirectToAction("VerifyPhone");
                    }
                }
                else
                {
                    // مستخدم جديد - إرسال رمز التحقق للتسجيل
                    var verificationCode = GenerateOTP();

                    // حفظ معلومات التحقق في الجلسة
                    HttpContext.Session.SetString("VerificationCode", verificationCode);
                    HttpContext.Session.SetString("PhoneNumber", cleanPhoneNumber);
                    HttpContext.Session.SetString("LoginType", "NewUser");

                    // إرسال رمز التحقق عبر WhatsApp
                    await _whatsAppService.SendPhoneVerificationAsync(cleanPhoneNumber, verificationCode, false);

                    TempData["InfoMessage"] = "رقم جديد! يرجى تأكيد رقم الهاتف أولاً.";
                    return RedirectToAction("VerifyPhone");
                }
            }

            return View(model);
        }

        // تحقق من OTP الأدمن
        [HttpGet]
        public IActionResult VerifyAdminOTP()
        {
            var adminPhone = HttpContext.Session.GetString("AdminPhone");
            var loginType = HttpContext.Session.GetString("LoginType");

            if (string.IsNullOrEmpty(adminPhone) || loginType != "Admin")
            {
                TempData["ErrorMessage"] = "جلسة التحقق منتهية الصلاحية.";
                return RedirectToAction("Login");
            }

            ViewBag.PhoneNumber = adminPhone;
            return View(new VerifyOTPViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyAdminOTP(VerifyOTPViewModel model)
        {
            if (ModelState.IsValid)
            {
                var storedOTP = HttpContext.Session.GetString("AdminOTP");
                var adminPhone = HttpContext.Session.GetString("AdminPhone");
                var loginType = HttpContext.Session.GetString("LoginType");

                if (model.OTP == storedOTP && !string.IsNullOrEmpty(adminPhone) && loginType == "Admin")
                {
                    // تسجيل دخول الأدمن
                    HttpContext.Session.SetString("UserId", "admin");
                    HttpContext.Session.SetString("UserName", "المدير العام");
                    HttpContext.Session.SetString("UserType", "Admin");
                    HttpContext.Session.SetString("IsLoggedIn", "true");

                    // تنظيف بيانات التحقق
                    HttpContext.Session.Remove("AdminOTP");
                    HttpContext.Session.Remove("AdminPhone");
                    HttpContext.Session.Remove("LoginType");

                    TempData["SuccessMessage"] = "مرحباً بك في لوحة التحكم!";
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }

                ModelState.AddModelError("", "رمز التحقق غير صحيح");
            }

            ViewBag.PhoneNumber = HttpContext.Session.GetString("AdminPhone");
            return View(model);
        }

        [HttpGet]
        public IActionResult VerifyOTP()
        {
            var phoneNumber = HttpContext.Session.GetString("PhoneNumber");
            var loginType = HttpContext.Session.GetString("LoginType");

            // التحقق من صحة الجلسة للمستخدمين المؤكدين
            if (string.IsNullOrEmpty(phoneNumber) ||
                (loginType != "ExistingVerifiedUser" && loginType != "ExistingUnverifiedUser"))
            {
                TempData["ErrorMessage"] = "جلسة التحقق منتهية الصلاحية. يرجى المحاولة مرة أخرى.";
                return RedirectToAction("Login");
            }

            var model = new VerifyOTPViewModel();
            ViewBag.PhoneNumber = phoneNumber;
            ViewBag.LoginType = loginType;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOTP(VerifyOTPViewModel model)
        {
            if (ModelState.IsValid)
            {
                var storedOTP = HttpContext.Session.GetString("OTP");
                var phoneNumber = HttpContext.Session.GetString("PhoneNumber");
                var loginType = HttpContext.Session.GetString("LoginType");
                var userId = HttpContext.Session.GetInt32("UserId");

                if (model.OTP == storedOTP && !string.IsNullOrEmpty(phoneNumber))
                {
                    if (loginType == "ExistingVerifiedUser" && userId.HasValue)
                    {
                        // دخول مستخدم مؤكد
                        var user = await _context.Users.FindAsync(userId.Value);
                        if (user != null)
                        {
                            await LoginUserAsync(user);
                            ClearVerificationSession();

                            TempData["SuccessMessage"] = $"مرحباً بعودتك {user.FirstName}!";
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else if (loginType == "ExistingUnverifiedUser" && userId.HasValue)
                    {
                        // تأكيد مستخدم غير مؤكد
                        var user = await _context.Users.FindAsync(userId.Value);
                        if (user != null)
                        {
                            user.IsPhoneVerified = true;
                            await _context.SaveChangesAsync();

                            await LoginUserAsync(user);
                            ClearVerificationSession();

                            TempData["SuccessMessage"] = $"تم تأكيد حسابك بنجاح! مرحباً بك {user.FirstName}";
                            return RedirectToAction("Index", "Home");
                        }
                    }
                }

                ModelState.AddModelError("", "رمز التحقق غير صحيح");
            }

            ViewBag.PhoneNumber = HttpContext.Session.GetString("PhoneNumber");
            ViewBag.LoginType = HttpContext.Session.GetString("LoginType");
            return View(model);
        }

        [HttpGet]
        public IActionResult VerifyPhone()
        {
            var phoneNumber = HttpContext.Session.GetString("PhoneNumber");
            var loginType = HttpContext.Session.GetString("LoginType");

            if (string.IsNullOrEmpty(phoneNumber) ||
                (loginType != "NewUser" && loginType != "ExistingUnverifiedUser"))
            {
                TempData["ErrorMessage"] = "جلسة التحقق منتهية الصلاحية.";
                return RedirectToAction("Login");
            }

            var model = new VerifyPhoneViewModel();
            ViewBag.PhoneNumber = phoneNumber;
            ViewBag.LoginType = loginType;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyPhone(VerifyPhoneViewModel model)
        {
            if (ModelState.IsValid)
            {
                var storedCode = HttpContext.Session.GetString("VerificationCode");
                var phoneNumber = HttpContext.Session.GetString("PhoneNumber");
                var loginType = HttpContext.Session.GetString("LoginType");

                if (model.VerificationCode == storedCode)
                {
                    if (loginType == "NewUser")
                    {
                        // التحقق من الهاتف ناجح للمستخدم الجديد - انتقال لإنشاء الحساب
                        HttpContext.Session.SetString("PhoneVerified", "true");
                        return RedirectToAction("CreateAccount");
                    }
                    else if (loginType == "ExistingUnverifiedUser")
                    {
                        // تأكيد المستخدم الموجود غير المؤكد
                        var userId = HttpContext.Session.GetInt32("UserId");
                        if (userId.HasValue)
                        {
                            var user = await _context.Users.FindAsync(userId.Value);
                            if (user != null)
                            {
                                user.IsPhoneVerified = true;
                                await _context.SaveChangesAsync();

                                await LoginUserAsync(user);
                                ClearVerificationSession();

                                TempData["SuccessMessage"] = $"تم تأكيد رقم هاتفك! مرحباً بك {user.FirstName}";
                                return RedirectToAction("Index", "Home");
                            }
                        }
                    }
                }

                ModelState.AddModelError("", "رمز التحقق غير صحيح");
            }

            ViewBag.PhoneNumber = HttpContext.Session.GetString("PhoneNumber");
            ViewBag.LoginType = HttpContext.Session.GetString("LoginType");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> CreateAccount()
        {
            var phoneNumber = HttpContext.Session.GetString("PhoneNumber");
            var phoneVerified = HttpContext.Session.GetString("PhoneVerified");
            var loginType = HttpContext.Session.GetString("LoginType");

            if (string.IsNullOrEmpty(phoneNumber) || phoneVerified != "true" || loginType != "NewUser")
            {
                TempData["ErrorMessage"] = "جلسة التحقق منتهية الصلاحية.";
                return RedirectToAction("Login");
            }

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            ViewBag.PhoneNumber = phoneNumber;

            var model = new CreateAccountViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAccount(CreateAccountViewModel model)
        {
            if (ModelState.IsValid)
            {
                var phoneNumber = HttpContext.Session.GetString("PhoneNumber");
                var phoneVerified = HttpContext.Session.GetString("PhoneVerified");
                var loginType = HttpContext.Session.GetString("LoginType");

                if (string.IsNullOrEmpty(phoneNumber) || phoneVerified != "true" || loginType != "NewUser")
                {
                    ModelState.AddModelError("", "جلسة التحقق منتهية الصلاحية");
                    return RedirectToAction("Login");
                }

                // التحقق من عدم وجود المستخدم (احتياط إضافي)
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);

                if (existingUser != null)
                {
                    ModelState.AddModelError("", "هذا الرقم مسجل مسبقاً");
                    return RedirectToAction("Login");
                }

                // التحقق من البريد الإلكتروني إذا تم إدخاله
                if (!string.IsNullOrEmpty(model.Email))
                {
                    var emailExists = await _context.Users
                        .AnyAsync(u => u.Email == model.Email);

                    if (emailExists)
                    {
                        ModelState.AddModelError("Email", "هذا البريد الإلكتروني مستخدم مسبقاً");
                        ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
                        return View(model);
                    }
                }

                // إنشاء المستخدم الجديد
                var user = new User
                {
                    PhoneNumber = phoneNumber,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender,
                    City = model.City,
                    District = model.District,
                    Location = model.Location,
                    Email = model.Email,
                    UserType = model.UserType,
                    StoreName = model.StoreName,
                    StoreDescription = model.StoreDescription,
                    WebsiteUrl1 = model.WebsiteUrl1,
                    WebsiteUrl2 = model.WebsiteUrl2,
                    WebsiteUrl3 = model.WebsiteUrl3,
                    IsPhoneVerified = true,
                    CreatedAt = DateTime.Now
                };

                // حفظ صورة الملف الشخصي إذا تم رفعها
                if (model.ProfileImage != null)
                {
                    var imagePath = await SaveProfileImageAsync(model.ProfileImage);
                    user.ProfileImage = imagePath;
                }

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // إضافة فئات المتجر إذا كان المستخدم بائع
                if (model.UserType == UserType.Seller && model.StoreCategories?.Any() == true)
                {
                    foreach (var categoryId in model.StoreCategories)
                    {
                        var storeCategory = new StoreCategory
                        {
                            UserId = user.Id,
                            CategoryId = categoryId,
                            CreatedAt = DateTime.Now
                        };
                        _context.StoreCategories.Add(storeCategory);
                    }
                    await _context.SaveChangesAsync();
                }

                // تسجيل دخول المستخدم
                await LoginUserAsync(user);

                // إرسال رسالة ترحيب
                await _whatsAppService.SendWelcomeMessageAsync(phoneNumber, user.FirstName, user.UserType.ToString());

                // تنظيف الجلسة
                ClearVerificationSession();

                // رسالة ترحيب
                TempData["SuccessMessage"] = $"مرحباً بك {user.FirstName}! تم إنشاء حسابك بنجاح";

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).ToListAsync();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResendCode()
        {
            try
            {
                var phoneNumber = HttpContext.Session.GetString("PhoneNumber");
                var adminPhone = HttpContext.Session.GetString("AdminPhone");
                var loginType = HttpContext.Session.GetString("LoginType");

                // إعادة إرسال للأدمن
                if (loginType == "Admin" && !string.IsNullOrEmpty(adminPhone))
                {
                    var newAdminOtp = GenerateOTP();
                    HttpContext.Session.SetString("AdminOTP", newAdminOtp);
                    await _whatsAppService.SendOTPAsync(adminPhone, newAdminOtp);
                    return Json(new { success = true, message = "تم إعادة إرسال رمز التحقق للأدمن" });
                }

                if (string.IsNullOrEmpty(phoneNumber))
                {
                    return Json(new { success = false, message = "جلسة منتهية الصلاحية" });
                }

                // إنشاء رمز جديد
                var newCode = GenerateOTP();

                if (loginType == "ExistingVerifiedUser")
                {
                    // إعادة إرسال OTP للمستخدم المؤكد
                    HttpContext.Session.SetString("OTP", newCode);
                    var userId = HttpContext.Session.GetInt32("UserId");
                    if (userId.HasValue)
                    {
                        var user = await _context.Users.FindAsync(userId.Value);
                        if (user != null)
                        {
                            await _whatsAppService.SendLoginOTPAsync(phoneNumber, newCode, user.FirstName);
                        }
                        else
                        {
                            await _whatsAppService.SendOTPAsync(phoneNumber, newCode);
                        }
                    }
                    else
                    {
                        await _whatsAppService.SendOTPAsync(phoneNumber, newCode);
                    }
                }
                else
                {
                    // إعادة إرسال رمز التحقق للمستخدمين الجدد أو غير المؤكدين
                    HttpContext.Session.SetString("VerificationCode", newCode);

                    bool isExistingUser = loginType == "ExistingUnverifiedUser";
                    await _whatsAppService.SendPhoneVerificationAsync(phoneNumber, newCode, isExistingUser);
                }

                return Json(new { success = true, message = "تم إعادة إرسال الرمز بنجاح" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"خطأ في إعادة الإرسال: {ex.Message}");
                return Json(new { success = false, message = "حدث خطأ في إعادة الإرسال" });
            }
        }

        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            TempData["InfoMessage"] = "تم تسجيل الخروج بنجاح";
            return RedirectToAction("Index", "Home");
        }

        #region Private Methods

        private async Task LoginUserAsync(User user)
        {
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserName", $"{user.FirstName} {user.LastName}");
            HttpContext.Session.SetString("UserType", user.UserType.ToString());
            HttpContext.Session.SetString("IsLoggedIn", "true");
        }

        private void ClearVerificationSession()
        {
            HttpContext.Session.Remove("OTP");
            HttpContext.Session.Remove("VerificationCode");
            HttpContext.Session.Remove("PhoneNumber");
            HttpContext.Session.Remove("LoginType");
            HttpContext.Session.Remove("PhoneVerified");
            HttpContext.Session.Remove("UserId");
            HttpContext.Session.Remove("AdminOTP");
            HttpContext.Session.Remove("AdminPhone");
        }

        private string GenerateOTP()
        {
            var random = new Random();
            return random.Next(1000, 9999).ToString();
        }

        private async Task<string?> SaveProfileImageAsync(IFormFile image)
        {
            try
            {
                if (image == null || image.Length == 0)
                    return null;

                // التحقق من نوع الملف
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                    return null;

                // التحقق من حجم الملف (5MB)
                if (image.Length > 5 * 1024 * 1024)
                    return null;

                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                return $"/uploads/profiles/{fileName}";
            }
            catch (Exception)
            {
                // في حالة حدوث خطأ، إرجاع null
                return null;
            }
        }

        #endregion
    }
}