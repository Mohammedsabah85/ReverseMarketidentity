using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Models.Identity;
using ReverseMarket.Services;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity Configuration
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    // Password settings
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters = "0123456789+";
    options.User.RequireUniqueEmail = false;

    // Email settings
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddRoles<ApplicationRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// Session Configuration
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Localization Configuration
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("ar-IQ"),
        new CultureInfo("en-US")
    };

    options.DefaultRequestCulture = new RequestCulture("ar-IQ");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;

    // Configure culture providers
    options.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider(),
        new QueryStringRequestCultureProvider()
    };
});

// Custom Services
builder.Services.AddScoped<ILanguageService, LanguageService>();
builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IFileService, FileService>();

// Email Configuration
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// File Upload Configuration
builder.Services.Configure<Dictionary<string, object>>("FileUploadSettings", builder.Configuration.GetSection("FileUploadSettings"));

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Localization
app.UseRequestLocalization();

app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Admin Area Route
app.MapControllerRoute(
    name: "admin",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Default Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// Database Seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Seed roles if they don't exist
        await SeedRolesAsync(roleManager);

        // Seed admin user if it doesn't exist
        await SeedAdminUserAsync(userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

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