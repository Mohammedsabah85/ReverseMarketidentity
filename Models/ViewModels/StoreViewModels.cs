namespace ReverseMarket.Models
{
    public class StoresViewModel
    {
        public List<ApplicationUserStore> Stores { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string? Search { get; set; }
        public int? SelectedCategoryId { get; set; }
    }

    // Helper class للعرض
    public class ApplicationUserStore
    {
        public string Id { get; set; } = "";
        public string StoreName { get; set; } = "";
        public string? StoreDescription { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string? Email { get; set; }
        public string City { get; set; } = "";
        public string District { get; set; } = "";
        public string? Location { get; set; }
        public string? ProfileImage { get; set; }
        public string? WebsiteUrl1 { get; set; }
        public string? WebsiteUrl2 { get; set; }
        public string? WebsiteUrl3 { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<StoreCategory> StoreCategories { get; set; } = new();

        public string FullName => $"{FirstName} {LastName}";
        public string DisplayName => !string.IsNullOrEmpty(StoreName) ? StoreName : FullName;
    }
}