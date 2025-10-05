using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ReverseMarket.CustomWhatsappService;
using ReverseMarket.Data;
using ReverseMarket.Extensions;
using ReverseMarket.Models;
using ReverseMarket.Models.Identity;
using ReverseMarket.Services;
using ReverseMarket.SignalR;
using System.Globalization;

using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddLocalization(options =>
{
    options.ResourcesPath = "Resources";
});

builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

// «··€«  «·„œ⁄Ê„…
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

    options.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new CookieRequestCultureProvider(),
        new QueryStringRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider()
    };
});





// ≈⁄œ«œ ﬁ«⁄œ… «·»Ì«‰« 
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddHttpContextAccessor();


builder.Services.AddScoped<ILanguageService, LanguageService>();

// Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.IsEssential = true;
});





// ≈⁄œ«œ Identity „⁄ «·√œÊ«—
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




////// ≈⁄œ«œ «· —Ã„… Ê«· œÊÌ·
////var supportedCultures = new[]
////{
////    new CultureInfo("ar-IQ"),
////    new CultureInfo("en-US"),
////    new CultureInfo("ku")
////};

////builder.Services.Configure<RequestLocalizationOptions>(options =>
////{
////    options.DefaultRequestCulture = new RequestCulture("ar-IQ");
////    options.SupportedCultures = supportedCultures;
////    options.SupportedUICultures = supportedCultures;

////    options.RequestCultureProviders = new List<IRequestCultureProvider>
////    {
////        new CookieRequestCultureProvider(),
////        new AcceptLanguageHeaderRequestCultureProvider()
////    };
////});

////builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// ≈⁄œ«œ «·Œœ„«  «·„Œ’’…
builder.Services.AddScoped<ILanguageService, LanguageService>();
builder.Services.AddHttpContextAccessor();

// ≈⁄œ«œ MVC Ê«·’›Õ« 
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// ≈⁄œ«œ Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ≈÷«›… Œœ„«  «· ÿ»Ìﬁ
builder.Services.AddApplicationServices(builder.Configuration);

// ›Ì »Ì∆… «· ÿÊÌ— ›ﬁÿ
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
}

// for real time chat : 
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, NameUserIdProvider>();

// custome whatsapp message : 
builder.Services.Configure<WhatsSettings>(
    builder.Configuration.GetSection("WhatsAppSettings"));
builder.Services.AddHttpClient<WhatsAppService>();

var app = builder.Build();


var localizationOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
app.UseRequestLocalization(localizationOptions);
// ≈⁄œ«œ Pipeline ··ÿ·»« 
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

//  — Ì» „Â„: Localization ﬁ»· Authentication
app.UseRequestLocalization();

app.UseAuthentication();
app.UseAuthorization();

// for real time chat : 
app.MapHub<ChatHub>("/chathub");

app.UseSession();

// ≈⁄œ«œ «·„”«—« 
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

//  ÂÌ∆… «·»Ì«‰«  «·√”«”Ì…
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

// œÊ«·  ÂÌ∆… «·»Ì«‰«  «·√”«”Ì…
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
