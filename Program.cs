// Program.cs - Updated for ASP.NET Core Identity
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ReverseMarket.Data;
using ReverseMarket.Models.Identity;
using ReverseMarket.Services;
using Microsoft.AspNetCore.Localization;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// ����� ����� ��������
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ����� Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // ������� ���� ������ (���� - ���� ������� ������ ����� OTP)
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;

    // ������� ��������
    options.User.RequireUniqueEmail = false;
    options.User.AllowedUserNameCharacters = "0123456789+";

    // ������� ����� ������
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;

    // ������� �����
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ����� Cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// ����� Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ����� Localization
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

    options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider());
});

// ����� �������
builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();
builder.Services.AddScoped<ILanguageService, LanguageService>();

// ����� MVC
builder.Services.AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization();

var app = builder.Build();

// ����� pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// ����� Localization middleware
app.UseRequestLocalization();

app.UseRouting();

// ����� Identity middleware
app.UseAuthentication();
app.UseAuthorization();

// ����� Session
app.UseSession();

// ����� ��������
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ����� ����� �������� ��������
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

        await SeedDatabaseAsync(context, userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "��� �� ����� ����� ��������");
    }
}

app.Run();

// ���� ����� ����� ��������
static async Task SeedDatabaseAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
{
    // ����� ����� �������� ��� �� ��� ������
    await context.Database.EnsureCreatedAsync();

    // ����� �������
    var roles = new[]
    {
        new ApplicationRole { Name = "Admin", Description = "���� ������" },
        new ApplicationRole { Name = "Seller", Description = "����/���� ����" },
        new ApplicationRole { Name = "Buyer", Description = "�����/����" }
    };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role.Name))
        {
            await roleManager.CreateAsync(role);
        }
    }

    // ����� ������ ��� �� ��� �������
    var adminPhone = "+9647700227210";
    var adminUser = await userManager.FindByNameAsync(adminPhone);

    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminPhone,
            PhoneNumber = adminPhone,
            FirstName = "����",
            LastName = "������",
            Email = "admin@reversemarket.iq",
            DateOfBirth = new DateTime(1990, 1, 1),
            Gender = "���",
            City = "�����",
            District = "�������",
            UserType = UserType.Buyer,
            PhoneNumberConfirmed = true,
            EmailConfirmed = true,
            IsPhoneVerified = true,
            IsEmailVerified = true,
            IsActive = true
        };

        var result = await userManager.CreateAsync(adminUser, "Admin@123456");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }

    // ����� ������� ������ ����������
    if (!await context.SiteSettings.AnyAsync())
    {
        var siteSettings = new ReverseMarket.Models.SiteSettings
        {
            ContactPhone = "+9647700227210",
            ContactWhatsApp = "+9647700227210",
            ContactEmail = "info@reversemarket.iq",
            AboutUs = "���� ����� ������ - ���� �������� ���������",
            CopyrightInfo = "� 2025 ����� ������. ���� ������ ������.",
            PrivacyPolicy = "����� �������� ��� �������",
            TermsOfUse = "���� ��������� ��� �������"
        };

        context.SiteSettings.Add(siteSettings);
        await context.SaveChangesAsync();
    }

    // ����� ���� ��������
    if (!await context.Categories.AnyAsync())
    {
        var categories = new[]
        {
            new ReverseMarket.Models.Category { Name = "����������", Description = "����� ��������� ������" },
            new ReverseMarket.Models.Category { Name = "����� ��������", Description = "����� ����������" },
            new ReverseMarket.Models.Category { Name = "���� ������", Description = "���� ��������� ������" },
            new ReverseMarket.Models.Category { Name = "������ �������", Description = "������ ���� ����" },
            new ReverseMarket.Models.Category { Name = "�����", Description = "����� ������" }
        };

        context.Categories.AddRange(categories);
        await context.SaveChangesAsync();
    }
}