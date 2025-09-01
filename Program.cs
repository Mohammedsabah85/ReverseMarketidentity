using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Extensions;
using ReverseMarket.Models.Identity;
using ReverseMarket.Services;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// ≈⁄œ«œ Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ≈÷«›… Identity Services
builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 4;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddRoles<ApplicationRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// ›Ì »Ì∆… «· ÿÊÌ— ›ﬁÿ
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
}

// ≈÷«›… MVC
builder.Services.AddControllersWithViews();

// ≈÷«›… Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ≈÷«›… Œœ„«  «· ÿ»Ìﬁ
builder.Services.AddApplicationServices(builder.Configuration);

// ≈⁄œ«œ Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

var supportedCultures = new[]
{
    new CultureInfo("ar"),
    new CultureInfo("en"),
    new CultureInfo("ku")
};

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("ar");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

var app = builder.Build();

// ≈⁄œ«œ Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    // ›Ì »Ì∆… «· ÿÊÌ— ›ﬁÿ
    app.UseMigrationsEndPoint();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseRequestLocalization();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();

// Seeding Methods
static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
{
    var roles = new[]
    {
        new { Name = "Admin", Description = "„œÌ— «·‰Ÿ«„" },
        new { Name = "Seller", Description = "»«∆⁄/’«Õ» „ Ã—" },
        new { Name = "Buyer", Description = "„‘ —Ì/⁄„Ì·" }
    };

    foreach (var roleInfo in roles)
    {
        if (!await roleManager.RoleExistsAsync(roleInfo.Name))
        {
            var role = new ApplicationRole
            {
                Name = roleInfo.Name,
                Description = roleInfo.Description,
                CreatedAt = DateTime.Now
            };

            await roleManager.CreateAsync(role);
        }
    }
}

static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
{
    var adminPhone = "+9647700227210";
    var adminUser = await userManager.FindByNameAsync(adminPhone);

    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminPhone,
            PhoneNumber = adminPhone,
            Email = "admin@reversemarket.iq",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            FirstName = "„œÌ—",
            LastName = "«·‰Ÿ«„",
            DateOfBirth = new DateTime(1990, 1, 1),
            Gender = "–ﬂ—",
            City = "»€œ«œ",
            District = "«·ﬂ—«œ…",
            UserType = UserType.Buyer, // Admin doesn't need specific user type
            IsPhoneVerified = true,
            IsEmailVerified = true,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        var result = await userManager.CreateAsync(adminUser, "Admin@123");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
    else
    {
        // Ensure admin has the Admin role
        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}