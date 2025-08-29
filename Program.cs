// Program.cs - Updated for Identity
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Extensions;
using ReverseMarket.Models.Identity;
using ReverseMarket.Services;
using ReverseMarket.Resources;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity services
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // Password settings
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredUniqueChars = 0;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = false; // Phone number is primary identifier

    // Sign in settings
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = true; // Require phone confirmation
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireSellerRole", policy => policy.RequireRole("Seller"));
    options.AddPolicy("RequireBuyerRole", policy => policy.RequireRole("Buyer"));
    options.AddPolicy("RequireVerifiedPhone", policy =>
        policy.RequireClaim("PhoneNumberConfirmed", "true"));
});

// Add localization
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new List<CultureInfo>
    {
        new CultureInfo("ar-IQ"), // Arabic (Iraq)
        new CultureInfo("en-US"), // English
        new CultureInfo("ku-IQ")  // Kurdish (Iraq)
    };

    options.DefaultRequestCulture = new RequestCulture("ar-IQ");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;

    // Add culture providers
    options.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());
    options.RequestCultureProviders.Insert(1, new CookieRequestCultureProvider());
    options.RequestCultureProviders.Insert(2, new AcceptLanguageHeaderRequestCultureProvider());
});

// Add custom services
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddScoped<ILanguageService, LanguageService>();

// Add session for temporary data during registration
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add MVC
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(SharedResource));
    });

// Add HTTP context accessor
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Use localization
app.UseRequestLocalization();

// Use session before authentication
app.UseSession();

// Use Identity authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Map default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map admin area route
app.MapControllerRoute(
    name: "admin",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

    await context.Database.MigrateAsync();

    // Seed data if needed
    await SeedDatabase(userManager, roleManager);
}

app.Run();

// Helper method to seed admin user if not exists
async Task SeedDatabase(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
{
    // Ensure roles exist
    var roles = new[] { "Admin", "Seller", "Buyer" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new ApplicationRole
            {
                Name = role,
                Description = role switch
                {
                    "Admin" => "„œÌ— «·‰Ÿ«„",
                    "Seller" => "»«∆⁄/’«Õ» „ Ã—",
                    "Buyer" => "„‘ —Ì/⁄„Ì·",
                    _ => role
                }
            });
        }
    }

    // Create admin user if not exists
    var adminPhone = "+9647700227210";
    var adminUser = await userManager.FindByNameAsync(adminPhone);

    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminPhone,
            PhoneNumber = adminPhone,
            Email = "admin@reversemarket.iq",
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
            PhoneNumberConfirmed = true,
            EmailConfirmed = true,
            CreatedAt = DateTime.Now
        };

        var result = await userManager.CreateAsync(adminUser, "Admin@123");

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");

            // Add phone confirmation claim
            await userManager.AddClaimAsync(adminUser, new System.Security.Claims.Claim("PhoneNumberConfirmed", "true"));
        }
    }
}


//using Microsoft.EntityFrameworkCore;
//using Microsoft.AspNetCore.Localization;
//using Microsoft.Extensions.Options;
//using ReverseMarket.Data;
//using ReverseMarket.Services;
//using System.Globalization;

//var builder = WebApplication.CreateBuilder(args);

//// Add Entity Framework
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//// Add session support
//builder.Services.AddDistributedMemoryCache();
//builder.Services.AddSession(options =>
//{
//    options.IdleTimeout = TimeSpan.FromMinutes(30);
//    options.Cookie.HttpOnly = true;
//    options.Cookie.IsEssential = true;
//});

//// Configure localization
//builder.Services.Configure<RequestLocalizationOptions>(options =>
//{
//    var supportedCultures = new[]
//    {
//        new CultureInfo("ar"),
//        new CultureInfo("en"),
//        new CultureInfo("ku")
//    };

//    options.DefaultRequestCulture = new RequestCulture("ar");
//    options.SupportedCultures = supportedCultures;
//    options.SupportedUICultures = supportedCultures;

//    // Configure culture providers
//    options.RequestCultureProviders.Clear();
//    options.RequestCultureProviders.Add(new QueryStringRequestCultureProvider());

//    // Use simple cookie provider without complex options
//    options.RequestCultureProviders.Add(new CookieRequestCultureProvider());

//    options.RequestCultureProviders.Add(new AcceptLanguageHeaderRequestCultureProvider());
//});

//// Add localization services
//builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

//// Add MVC with views
//builder.Services.AddControllersWithViews()
//    .AddViewLocalization()
//    .AddDataAnnotationsLocalization();

//// Register HTTP context accessor
//builder.Services.AddHttpContextAccessor();

//// Register custom services
//builder.Services.AddScoped<ILanguageService, LanguageService>();
//builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();
//builder.Services.AddScoped<IEmailService, EmailService>();
//builder.Services.AddScoped<IFileService, FileService>();

//// Configure email settings
//builder.Services.Configure<EmailSettings>(
//    builder.Configuration.GetSection("EmailSettings"));

//// Configure WhatsApp settings  
//builder.Services.Configure<WhatsAppSettings>(
//    builder.Configuration.GetSection("WhatsAppSettings"));

//// Add HTTP client for WhatsApp service
//builder.Services.AddHttpClient<WhatsAppService>();

//var app = builder.Build();

//// Configure the HTTP request pipeline
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Home/Error");
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();

//app.UseRouting();

//// Add localization middleware BEFORE authorization
//var localizationOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>();
//app.UseRequestLocalization(localizationOptions.Value);

//app.UseSession();
//app.UseAuthorization();

//// Configure routes
//app.MapControllerRoute(
//    name: "areas",
//    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Home}/{action=Index}/{id?}");

//app.Run();

////using Microsoft.AspNetCore.Localization;
////using Microsoft.AspNetCore.Mvc.Razor;
////using Microsoft.EntityFrameworkCore;
////using Microsoft.Extensions.Options;
////using ReverseMarket.Data;
////using ReverseMarket.Extensions;
////using ReverseMarket.Services;
////using System.Globalization;


////var builder = WebApplication.CreateBuilder(args);

////// Add DbContext
////builder.Services.AddDbContext<ApplicationDbContext>(options =>
////    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

////builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");


////// Add Session with enhanced configuration
////builder.Services.AddSession(options =>
////{
////    options.IdleTimeout = TimeSpan.FromMinutes(60); //  „œÌœ „œ… «·Ã·”… ≈·Ï 60 œﬁÌﬁ…
////    options.Cookie.HttpOnly = true;
////    options.Cookie.IsEssential = true;
////    options.Cookie.Name = "ReverseMarket.Session";
////    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
////});




////var supportedCultures = new[]
////{
////    new CultureInfo("ar"),
////    new CultureInfo("en"),
////    new CultureInfo("ku")
////};

////builder.Services.Configure<RequestLocalizationOptions>(options =>
////{
////    options.DefaultRequestCulture = new RequestCulture("ar");
////    options.SupportedCultures = supportedCultures;
////    options.SupportedUICultures = supportedCultures;

////    //  — Ì» „ﬁœ„Ì «·Àﬁ«›…
////    options.RequestCultureProviders = new List<IRequestCultureProvider>
////    {
////        new CookieRequestCultureProvider(),
////        new QueryStringRequestCultureProvider(),
////        new AcceptLanguageHeaderRequestCultureProvider()
////    };
////});

////// ≈÷«›… «· ÊÿÌ‰

////builder.Services.AddControllersWithViews()
////    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
////    .AddDataAnnotationsLocalization();

////// Add Controllers and Views
////builder.Services.AddControllersWithViews(options =>
////{
////    // ≈÷«›… ›· —«  ⁄«„…
////    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
////});

////builder.Services.AddScoped<ILanguageService, LanguageService>();
////builder.Services.AddHttpContextAccessor();
////// Add Services
////builder.Services.Configure<WhatsAppSettings>(builder.Configuration.GetSection("WhatsAppSettings"));
////builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

////builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();
////builder.Services.AddScoped<IFileService, FileService>();
////builder.Services.AddScoped<IEmailService, EmailService>();
////builder.Services.AddHttpClient<WhatsAppService>();

////// Add Memory Cache
////builder.Services.AddMemoryCache();

////// Add Logging
////builder.Services.AddLogging(config =>
////{
////    config.AddConsole();
////    config.AddDebug();
////});

////var app = builder.Build();

////var localizationOptions = app.Services.GetService<IOptions<RequestLocalizationOptions>>()!.Value;
////app.UseRequestLocalization(localizationOptions);

////// Configure the HTTP request pipeline
////if (!app.Environment.IsDevelopment())
////{
////    app.UseExceptionHandler("/Home/Error");
////    app.UseHsts();
////}
////else
////{
////    app.UseDeveloperExceptionPage();
////}

////app.UseHttpsRedirection();
////app.UseStaticFiles();


////// ≈⁄œ«œ œ⁄„ «·’Ê— Ê«·„·›«  «·„—›Ê⁄…
////app.UseStaticFiles(new StaticFileOptions
////{
////    RequestPath = "/uploads",
////    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
////        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads")),
////    ServeUnknownFileTypes = false,
////    DefaultContentType = "application/octet-stream"
////});
////app.UseRouting();
////app.UseSession(); // ÌÃ» √‰ ÌﬂÊ‰ ﬁ»· UseAuthorization
////app.UseAuthorization();

////// Configure routes
////app.MapControllerRoute(
////    name: "admin_areas",
////    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

////app.MapControllerRoute(
////    name: "admin_subcategory1",
////    pattern: "Admin/Categories/CreateSubCategory1/{categoryId:int}",
////    defaults: new { area = "Admin", controller = "Categories", action = "CreateSubCategory1" });

////app.MapControllerRoute(
////    name: "admin_subcategory2",
////    pattern: "Admin/Categories/CreateSubCategory2/{subCategory1Id:int}",
////    defaults: new { area = "Admin", controller = "Categories", action = "CreateSubCategory2" });

////// Routes ··Õ”«»
////app.MapControllerRoute(
////    name: "account",
////    pattern: "Account/{action=Login}",
////    defaults: new { controller = "Account" });
////app.MapControllerRoute(
////    name: "localized",
////    pattern: "{culture}/{controller=Home}/{action=Index}/{id?}",
////    constraints: new { culture = @"^(ar|en|ku)$" });

////app.MapControllerRoute(
////    name: "default",
////    pattern: "{controller=Home}/{action=Index}/{id?}");
////CreateUploadDirectories(app.Environment.WebRootPath);

////// ≈‰‘«¡ ﬁ«⁄œ… «·»Ì«‰«  ≈–« ·„  ﬂ‰ „ÊÃÊœ…
////using (var scope = app.Services.CreateScope())
////{
////    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
////    try
////    {
////        context.Database.EnsureCreated();
////    }
////    catch (Exception ex)
////    {
////        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
////        logger.LogError(ex, "ÕœÀ Œÿ√ √À‰«¡ ≈‰‘«¡ ﬁ«⁄œ… «·»Ì«‰« ");
////    }
////}

////app.Run();
//void CreateUploadDirectories(string webRootPath)
//{
//    var uploadPaths = new[]
//    {
//        Path.Combine(webRootPath, "uploads"),
//        Path.Combine(webRootPath, "uploads", "requests"),
//        Path.Combine(webRootPath, "uploads", "profiles"),
//        Path.Combine(webRootPath, "uploads", "advertisements"),
//        Path.Combine(webRootPath, "uploads", "site"),
//        Path.Combine(webRootPath, "logs")
//    };

//    foreach (var path in uploadPaths)
//    {
//        if (!Directory.Exists(path))
//        {
//            Directory.CreateDirectory(path);
//            Console.WriteLine($"Created directory: {path}");
//        }
//    }
//}