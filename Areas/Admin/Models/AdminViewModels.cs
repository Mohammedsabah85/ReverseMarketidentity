﻿using ReverseMarket.Models;
using ReverseMarket.Models.Identity; // الـ namespace الصحيح
using System.ComponentModel.DataAnnotations;

namespace ReverseMarket.Areas.Admin.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int TotalStores { get; set; }
        public int TotalCategories { get; set; }
        public List<Request> RecentRequests { get; set; } = new();
    }

    public class AdminRequestsViewModel
    {
        public List<Request> Requests { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public RequestStatus? StatusFilter { get; set; }
    }

    public class CreateAdvertisementViewModel
    {
        [Required(ErrorMessage = "عنوان الإعلان مطلوب")]
        [StringLength(255, ErrorMessage = "عنوان الإعلان لا يجب أن يزيد عن 255 حرف")]
        public string Title { get; set; } = "";

        [StringLength(1000, ErrorMessage = "وصف الإعلان لا يجب أن يزيد عن 1000 حرف")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "صورة الإعلان مطلوبة")]
        public IFormFile Image { get; set; } = null!;

        [Url(ErrorMessage = "رابط الإعلان غير صحيح")]
        public string? LinkUrl { get; set; }

        [Required(ErrorMessage = "نوع الإعلان مطلوب")]
        public AdvertisementType Type { get; set; }

        [Range(0, 999, ErrorMessage = "ترتيب العرض يجب أن يكون بين 0 و 999")]
        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "تاريخ البداية مطلوب")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }
    }

    public class AdminUsersViewModel
    {
        public List<UserViewModel> Users { get; set; } = new(); // تغيير من User إلى ApplicationUser


        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string? Search { get; set; }
        public UserType? UserTypeFilter { get; set; }
        public bool? IsActiveFilter { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int BuyersCount { get; set; }
        public int SellersCount { get; set; }
    }

    //public class EditUserViewModel
    //{
    //    public string Id { get; set; } = ""; // تغيير من int إلى string

    //    [Required(ErrorMessage = "الاسم الأول مطلوب")]
    //    [StringLength(50, ErrorMessage = "الاسم الأول لا يجب أن يزيد عن 50 حرف")]
    //    public string FirstName { get; set; } = "";

    //    [Required(ErrorMessage = "اسم العائلة مطلوب")]
    //    [StringLength(50, ErrorMessage = "اسم العائلة لا يجب أن يزيد عن 50 حرف")]
    //    public string LastName { get; set; } = "";

    //    [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
    //    public string? Email { get; set; }

    //    [Required(ErrorMessage = "رقم الهاتف مطلوب")]
    //    [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
    //    public string PhoneNumber { get; set; } = "";

    //    [Required(ErrorMessage = "المحافظة مطلوبة")]
    //    public string City { get; set; } = "";

    //    [Required(ErrorMessage = "المنطقة مطلوبة")]
    //    public string District { get; set; } = "";

    //    public string? Location { get; set; }

    //    [Required(ErrorMessage = "تاريخ الميلاد مطلوب")]
    //    public DateTime DateOfBirth { get; set; }

    //    [Required(ErrorMessage = "الجنس مطلوب")]
    //    public string Gender { get; set; } = "";

    //    public UserType UserType { get; set; }

    //    // بيانات المتجر (للبائعين)
    //    public string? StoreName { get; set; }
    //    public string? StoreDescription { get; set; }
    //    public string? WebsiteUrl1 { get; set; }
    //    public string? WebsiteUrl2 { get; set; }
    //    public string? WebsiteUrl3 { get; set; }

    //    // فئات المتجر
    //    public List<int> StoreCategories { get; set; } = new();
    //    public List<int> CurrentStoreCategories { get; set; } = new();

    //    // حالة الحساب
    //    public bool IsActive { get; set; } = true;
    //    public bool IsPhoneVerified { get; set; } = true;
    //}

    public class UserDetailsViewModel
    {
        public ApplicationUser User { get; set; } = new();
        public UserStatistics Statistics { get; set; } = new();
        public List<Request> RecentRequests { get; set; } = new();
    }

    public class UserStatistics
    {
        public int TotalRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int PendingRequests { get; set; }
        public int RejectedRequests { get; set; }
        public DateTime LastActivity { get; set; }
        public int DaysSinceRegistration { get; set; }
    }
}