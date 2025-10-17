using System;
using System.Collections.Generic;
using System.Linq;
using ReverseMarket.Models;
using ReverseMarket.Models.Identity;

namespace ReverseMarket.Extensions
{
    public static class UserExtensions
    {
        /// <summary>
        /// تحويل ApplicationUser إلى User DTO
        /// </summary>
        public static User ToUserModel(this ApplicationUser appUser)
        {
            if (appUser == null) 
                return null;
            
            return new User
            {
                // المشكلة الأساسية: التأكد من نسخ Id كـ string
                Id = appUser.Id, // ASP.NET Identity يستخدم string للـ Id
                FirstName = appUser.FirstName ?? string.Empty,
                LastName = appUser.LastName ?? string.Empty,
                Email = appUser.Email ?? string.Empty,
                PhoneNumber = appUser.PhoneNumber ?? string.Empty,
                City = appUser.City ?? string.Empty,
                District = appUser.District ?? string.Empty,
                Location = appUser.Location ?? string.Empty,
                DateOfBirth = appUser.DateOfBirth,
                Gender = appUser.Gender ?? string.Empty,
                UserType = appUser.UserType,
                StoreName = appUser.StoreName ?? string.Empty,
                StoreDescription = appUser.StoreDescription ?? string.Empty,
                WebsiteUrl1 = appUser.WebsiteUrl1 ?? string.Empty,
                WebsiteUrl2 = appUser.WebsiteUrl2 ?? string.Empty,
                WebsiteUrl3 = appUser.WebsiteUrl3 ?? string.Empty,
                ProfileImage = appUser.ProfileImage ?? string.Empty,
                IsActive = appUser.IsActive,
                IsPhoneVerified = appUser.IsPhoneVerified,
                PhoneNumberConfirmed = appUser.PhoneNumberConfirmed,
                CreatedAt = appUser.CreatedAt,
                UpdatedAt = appUser.UpdatedAt,
                StoreCategories = appUser.StoreCategories?.ToList() ?? new List<StoreCategory>()
            };
        }

        /// <summary>
        /// تحويل مجموعة من ApplicationUsers إلى مجموعة من User DTOs
        /// </summary>
        public static List<User> ToUserModels(this IEnumerable<ApplicationUser> appUsers)
        {
            if (appUsers == null)
                return new List<User>();
            
            return appUsers
                .Where(u => u != null)
                .Select(u => u.ToUserModel())
                .Where(u => u != null)
                .ToList();
        }

        /// <summary>
        /// التحقق من أن المستخدم هو الأدمن
        /// </summary>
        public static bool IsAdmin(this ApplicationUser user)
        {
            return user?.PhoneNumber == "+9647700227210";
        }

        /// <summary>
        /// الحصول على الاسم الكامل للمستخدم
        /// </summary>
        public static string GetFullName(this ApplicationUser user)
        {
            if (user == null)
                return string.Empty;
            
            return $"{user.FirstName} {user.LastName}".Trim();
        }

        /// <summary>
        /// الحصول على اسم العرض (اسم المتجر للبائعين أو الاسم الكامل)
        /// </summary>
        public static string GetDisplayName(this ApplicationUser user)
        {
            if (user == null)
                return string.Empty;
            
            if (user.UserType == UserType.Seller && !string.IsNullOrEmpty(user.StoreName))
                return user.StoreName;
            
            return user.GetFullName();
        }
    }

    /// <summary>
    /// User DTO Model
    /// </summary>
    public class User
    {
        // تغيير مهم: Id يجب أن يكون string وليس int
        public string Id { get; set; } // <-- هذا هو السبب الرئيسي للمشكلة
        
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string City { get; set; }
        public string District { get; set; }
        public string Location { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public UserType UserType { get; set; }
        
        // Store Information
        public string StoreName { get; set; }
        public string StoreDescription { get; set; }
        public string WebsiteUrl1 { get; set; }
        public string WebsiteUrl2 { get; set; }
        public string WebsiteUrl3 { get; set; }
        public string ProfileImage { get; set; }
        
        // Account Status
        public bool IsActive { get; set; }
        public bool IsPhoneVerified { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        
        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
        // Relations
        public List<StoreCategory> StoreCategories { get; set; }

        // Constructor
        public User()
        {
            StoreCategories = new List<StoreCategory>();
            Id = string.Empty; // Initialize with empty string
        }

        // Static Factory Methods (للتوافق مع الكود الموجود)
        public static User FromApplicationUser(ApplicationUser appUser)
        {
            return appUser?.ToUserModel();
        }

        public static List<User> FromApplicationUsers(IEnumerable<ApplicationUser> appUsers)
        {
            return appUsers?.ToUserModels() ?? new List<User>();
        }

        // Helper Properties
        public string FullName => $"{FirstName} {LastName}".Trim();
        
        public bool IsAdmin => PhoneNumber == "+9647700227210";
        
        public string DisplayName => UserType == UserType.Seller && !string.IsNullOrEmpty(StoreName) 
            ? StoreName 
            : FullName;

        // Override for debugging
        public override string ToString()
        {
            return $"User[Id={Id}, Name={FullName}, Phone={PhoneNumber}, Type={UserType}, Active={IsActive}]";
        }
    }
}
