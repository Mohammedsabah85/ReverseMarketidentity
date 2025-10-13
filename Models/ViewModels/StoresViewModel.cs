using ReverseMarket.Models;
using ReverseMarket.Models.Identity;

public class StoresViewModel
{
    public List<ApplicationUser> Stores { get; set; } = new(); // تغيير من User إلى ApplicationUser
    public List<Category> Categories { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public string? Search { get; set; }
    public int? SelectedCategoryId { get; set; }
}