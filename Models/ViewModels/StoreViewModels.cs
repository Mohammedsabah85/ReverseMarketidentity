namespace ReverseMarket.Models
{
    public class StoresViewModel
    {
        public List<User> Stores { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string? Search { get; set; }
        public int? SelectedCategoryId { get; set; }
    }
}