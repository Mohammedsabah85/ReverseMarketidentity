//using ReverseMarket.Models;
//using ReverseMarket.Models.Identity;

//namespace ReverseMarket.Extensions
//{
//    // User DTO class للعرض
//    public class User
//    {
//        public string Id { get; set; } = "";
//        public string FirstName { get; set; } = "";
//        public string LastName { get; set; } = "";
//        public string? Email { get; set; }
//        public string PhoneNumber { get; set; } = "";
//        public string City { get; set; } = "";
//        public string District { get; set; } = "";
//        public string? Location { get; set; }
//        public DateTime DateOfBirth { get; set; }
//        public string Gender { get; set; } = "";
//        public UserType UserType { get; set; }
//        public string? StoreName { get; set; }
//        public string? StoreDescription { get; set; }
//        public string? WebsiteUrl1 { get; set; }
//        public string? WebsiteUrl2 { get; set; }
//        public string? WebsiteUrl3 { get; set; }
//        public string? ProfileImage { get; set; }
//        public bool IsActive { get; set; }
//        public bool IsPhoneVerified { get; set; }
//        public bool IsEmailVerified { get; set; }
//        public DateTime CreatedAt { get; set; }
//        public DateTime? UpdatedAt { get; set; }
//        public List<StoreCategory> StoreCategories { get; set; } = new();

//        // Extension methods للتحويل
//        public static User FromApplicationUser(ApplicationUser appUser)
//        {
//            return new User
//            {
//                Id = appUser.Id,
//                FirstName = appUser.FirstName,
//                LastName = appUser.LastName,
//                Email = appUser.Email,
//                PhoneNumber = appUser.PhoneNumber ?? "",
//                City = appUser.City,
//                District = appUser.District,
//                Location = appUser.Location,
//                DateOfBirth = appUser.DateOfBirth,
//                Gender = appUser.Gender,
//                UserType = appUser.UserType,
//                StoreName = appUser.StoreName,
//                StoreDescription = appUser.StoreDescription,
//                WebsiteUrl1 = appUser.WebsiteUrl1,
//                WebsiteUrl2 = appUser.WebsiteUrl2,
//                WebsiteUrl3 = appUser.WebsiteUrl3,
//                ProfileImage = appUser.ProfileImage,
//                IsActive = appUser.IsActive,
//                IsPhoneVerified = appUser.IsPhoneVerified,
//                IsEmailVerified = appUser.IsEmailVerified,
//                CreatedAt = appUser.CreatedAt,
//                UpdatedAt = appUser.UpdatedAt,
//                StoreCategories = appUser.StoreCategories?.ToList() ?? new List<StoreCategory>()
//            };
//        }

//        public static List<User> FromApplicationUsers(List<ApplicationUser> appUsers)
//        {
//            return appUsers.Select(FromApplicationUser).ToList();
//        }
//    }
//}