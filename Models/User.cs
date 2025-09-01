// Models/User.cs - User model as DTO for views compatibility
using System.ComponentModel.DataAnnotations;
using ReverseMarket.Models.Identity;

namespace ReverseMarket.Models
{
    /// <summary>
    /// User DTO for view compatibility - maps from ApplicationUser
    /// </summary>
    public class User
    {
        public int Id { get; set; } // Note: ApplicationUser uses string ID, this is for compatibility

        [Required]
        public string PhoneNumber { get; set; } = "";

        [Required]
        public string FirstName { get; set; } = "";

        [Required]
        public string LastName { get; set; } = "";

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string Gender { get; set; } = "";

        [Required]
        public string City { get; set; } = "";

        [Required]
        public string District { get; set; } = "";

        public string? Location { get; set; }

        public string? Email { get; set; }

        public string? ProfileImage { get; set; }

        [Required]
        public UserType UserType { get; set; }

        public bool IsPhoneVerified { get; set; }

        public bool IsEmailVerified { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Store properties (if user is seller)
        public string? StoreName { get; set; }
        public string? StoreDescription { get; set; }
        public string? WebsiteUrl1 { get; set; }
        public string? WebsiteUrl2 { get; set; }
        public string? WebsiteUrl3 { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<Request> Requests { get; set; } = new List<Request>();
        public virtual ICollection<StoreCategory> StoreCategories { get; set; } = new List<StoreCategory>();

        // Helper properties
        public string FullName => $"{FirstName} {LastName}";
        public string DisplayName => UserType == UserType.Seller && !string.IsNullOrEmpty(StoreName)
            ? StoreName
            : FullName;

        /// <summary>
        /// Creates User DTO from ApplicationUser
        /// </summary>
        public static User FromApplicationUser(ApplicationUser appUser)
        {
            if (appUser == null) throw new ArgumentNullException(nameof(appUser));

            // Convert string ID to int (this is a workaround for view compatibility)
            var userId = int.TryParse(appUser.Id, out var intId) ? intId : 0;

            return new User
            {
                Id = userId,
                PhoneNumber = appUser.PhoneNumber ?? "",
                FirstName = appUser.FirstName,
                LastName = appUser.LastName,
                DateOfBirth = appUser.DateOfBirth,
                Gender = appUser.Gender,
                City = appUser.City,
                District = appUser.District,
                Location = appUser.Location,
                Email = appUser.Email,
                ProfileImage = appUser.ProfileImage,
                UserType = appUser.UserType,
                IsPhoneVerified = appUser.IsPhoneVerified,
                IsEmailVerified = appUser.IsEmailVerified,
                CreatedAt = appUser.CreatedAt,
                StoreName = appUser.StoreName,
                StoreDescription = appUser.StoreDescription,
                WebsiteUrl1 = appUser.WebsiteUrl1,
                WebsiteUrl2 = appUser.WebsiteUrl2,
                WebsiteUrl3 = appUser.WebsiteUrl3,
                IsActive = appUser.IsActive,
                UpdatedAt = appUser.UpdatedAt,
                StoreCategories = appUser.StoreCategories,
                Requests = appUser.Requests
            };
        }

        /// <summary>
        /// Creates list of User DTOs from ApplicationUser list
        /// </summary>
        public static List<User> FromApplicationUsers(IEnumerable<ApplicationUser> appUsers)
        {
            return appUsers?.Select(FromApplicationUser).ToList() ?? new List<User>();
        }
    }
}