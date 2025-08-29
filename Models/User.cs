using System.ComponentModel.DataAnnotations;

namespace ReverseMarket.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string Gender { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string District { get; set; }

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
    }

    public enum UserType
    {
        Buyer = 1,
        Seller = 2
    }
}

