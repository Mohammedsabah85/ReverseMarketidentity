using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ReverseMarket.CustomWhatsappService;
using ReverseMarket.Data;
using ReverseMarket.Extensions;
using ReverseMarket.Models;
using ReverseMarket.Models.Identity;
using ReverseMarket.Services;
using ReverseMarket.SignalR;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// ====================================
// 1. Database Configuration
// ====================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ====================================
// 2. Identity Configuration
// ====================================
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // ≈⁄œ«œ«   ”ÃÌ· «·œŒÊ·
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;

    // ≈⁄œ«œ«  ﬂ·„… «·„—Ê— („»”ÿ… ·· ÿÊÌ—)
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 4;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();
builder.Services.AddScoped<IEmailService, EmailService>();
// ====================================
// 3. Localization Configuration
// ====================================
////builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

////builder.Services.Configure<RequestLocalizationOptions>(options =>
////{
////    var supportedCultures = new[]
////    {
////        new CultureInfo("ar"),
////        new CultureInfo("en"),
////        new CultureInfo("ku")
////    };

////    options.DefaultRequestCulture = new RequestCulture("ar");
////    options.SupportedCultures = supportedCultures;
////    options.SupportedUICultures = supportedCultures;

////    //  — Ì» Providers „Â„ Ãœ« - Cookie √Ê·«
////    options.RequestCultureProviders = new List<IRequestCultureProvider>
////    {
////        new CookieRequestCultureProvider
////        {
////            CookieName = CookieRequestCultureProvider.DefaultCookieName
////        },
////        new QueryStringRequestCultureProvider(),
////        new AcceptLanguageHeaderRequestCultureProvider()
////    };
////});
///builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddControllersWithViews()
    .AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix)
    .AddDataAnnotationsLocalization();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new System.Globalization.CultureInfo("ar"),
        new System.Globalization.CultureInfo("en"),
        new System.Globalization.CultureInfo("ku")
    };

    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("ar");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;

    // ? „Â„:  — Ì» Providers
    options.RequestCultureProviders = new List<Microsoft.AspNetCore.Localization.IRequestCultureProvider>
    {
        new Microsoft.AspNetCore.Localization.CookieRequestCultureProvider
        {
            CookieName = ".AspNetCore.Culture"
        },
        new Microsoft.AspNetCore.Localization.QueryStringRequestCultureProvider(),
        new Microsoft.AspNetCore.Localization.AcceptLanguageHeaderRequestCultureProvider()
    };
});


// ====================================
// 4. MVC and Views Configuration
// ====================================
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

builder.Services.AddRazorPages();

// ====================================
// 5. Session Configuration
// ====================================
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ====================================
// 6. Application Services
// ====================================
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ILanguageService, LanguageService>();
builder.Services.AddApplicationServices(builder.Configuration);

// ====================================
// 7. SignalR for Real-time Chat
// ====================================
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, NameUserIdProvider>();

// ====================================
// 8. WhatsApp Service Configuration
// ====================================
builder.Services.Configure<WhatsSettings>(
    builder.Configuration.GetSection("WhatsAppSettings"));
builder.Services.AddHttpClient<WhatsAppService>();

// ====================================
// 9. Development Tools
// ====================================
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
}

// ====================================
// BUILD APP
// ====================================
var app = builder.Build();

// ====================================
// 10. Request Pipeline Configuration
// ====================================

// Localization (ÌÃ» √‰ ÌﬂÊ‰ Ê«Õœ ›ﬁÿ)
var localizationOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(localizationOptions);

// Error Handling
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Static Files
app.UseHttpsRedirection();
app.UseStaticFiles();

// Routing
app.UseRouting();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Session
app.UseSession();

// SignalR Hub
app.MapHub<ChatHub>("/chathub");

// Routes
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// ====================================
// 11. Data Seeding
// ====================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

        await SeedRolesAsync(roleManager);
        await SeedAdminUserAsync(userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "ÕœÀ Œÿ√ √À‰«¡  ÂÌ∆… «·»Ì«‰«  «·√”«”Ì…");
    }
}

app.Run();

// ====================================
// HELPER METHODS - Data Seeding
// ====================================

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

            var result = await roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                throw new Exception($"›‘· ›Ì ≈‰‘«¡ œÊ— {roleInfo.Name}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
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
            UserType = UserType.Buyer,
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
        else
        {
            throw new Exception($"›‘· ›Ì ≈‰‘«¡ «·„œÌ—: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
    else
    {
        // «· √ﬂœ „‰ √‰ «·„œÌ— ·œÌÂ œÊ— Admin
        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}