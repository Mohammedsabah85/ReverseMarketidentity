// Data/ApplicationDbContext.cs - Updated for Identity
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ReverseMarket.Models;
using ReverseMarket.Models.Identity;

namespace ReverseMarket.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // DbSets
        public DbSet<Category> Categories { get; set; }
        public DbSet<SubCategory1> SubCategories1 { get; set; }
        public DbSet<SubCategory2> SubCategories2 { get; set; }
        public DbSet<Request> Requests { get; set; }
        public DbSet<RequestImage> RequestImages { get; set; }
        public DbSet<StoreCategory> StoreCategories { get; set; }
        public DbSet<Advertisement> Advertisements { get; set; }
        public DbSet<SiteSettings> SiteSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Identity tables names (optional - keep English names or change to Arabic)
            modelBuilder.Entity<ApplicationUser>().ToTable("Users");
            modelBuilder.Entity<ApplicationRole>().ToTable("Roles");
            modelBuilder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
            modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
            modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
            modelBuilder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
            modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");

            // ApplicationUser configurations
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.FirstName).HasMaxLength(50).IsRequired();
                entity.Property(e => e.LastName).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Gender).HasMaxLength(10).IsRequired();
                entity.Property(e => e.City).HasMaxLength(100).IsRequired();
                entity.Property(e => e.District).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Location).HasMaxLength(255);
                entity.Property(e => e.ProfileImage).HasMaxLength(500);
                entity.Property(e => e.StoreName).HasMaxLength(255);
                entity.Property(e => e.StoreDescription).HasMaxLength(1000);
                entity.Property(e => e.WebsiteUrl1).HasMaxLength(500);
                entity.Property(e => e.WebsiteUrl2).HasMaxLength(500);
                entity.Property(e => e.WebsiteUrl3).HasMaxLength(500);

                // Index for phone number uniqueness (PhoneNumber is already indexed by Identity)
                entity.HasIndex(e => e.PhoneNumber).IsUnique();
            });

            // Category configurations
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.ImagePath).HasMaxLength(500);
            });

            modelBuilder.Entity<SubCategory1>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(255).IsRequired();

                entity.HasOne(e => e.Category)
                    .WithMany(e => e.SubCategories1)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SubCategory2>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(255).IsRequired();

                entity.HasOne(e => e.SubCategory1)
                    .WithMany(e => e.SubCategories2)
                    .HasForeignKey(e => e.SubCategory1Id)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Request configurations
            modelBuilder.Entity<Request>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(2000).IsRequired();
                entity.Property(e => e.City).HasMaxLength(100).IsRequired();
                entity.Property(e => e.District).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Location).HasMaxLength(255);
                entity.Property(e => e.AdminNotes).HasMaxLength(1000);

                // Relationship with ApplicationUser instead of User
                entity.HasOne<ApplicationUser>()
                    .WithMany(u => u.Requests)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Category)
                    .WithMany(e => e.Requests)
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.SubCategory1)
                    .WithMany(e => e.Requests)
                    .HasForeignKey(e => e.SubCategory1Id)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.SubCategory2)
                    .WithMany(e => e.Requests)
                    .HasForeignKey(e => e.SubCategory2Id)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RequestImage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ImagePath).HasMaxLength(500).IsRequired();

                entity.HasOne(e => e.Request)
                    .WithMany(e => e.Images)
                    .HasForeignKey(e => e.RequestId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // StoreCategory configurations
            modelBuilder.Entity<StoreCategory>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Relationship with ApplicationUser instead of User
                entity.HasOne<ApplicationUser>()
                    .WithMany(u => u.StoreCategories)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Category)
                    .WithMany()
                    .HasForeignKey(e => e.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.SubCategory1)
                    .WithMany()
                    .HasForeignKey(e => e.SubCategory1Id)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.SubCategory2)
                    .WithMany()
                    .HasForeignKey(e => e.SubCategory2Id)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Advertisement configurations
            modelBuilder.Entity<Advertisement>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.ImagePath).HasMaxLength(500).IsRequired();
                entity.Property(e => e.LinkUrl).HasMaxLength(500);
            });

            // SiteSettings configurations
            modelBuilder.Entity<SiteSettings>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SiteLogo).HasMaxLength(500);
                entity.Property(e => e.ContactPhone).HasMaxLength(20);
                entity.Property(e => e.ContactWhatsApp).HasMaxLength(20);
                entity.Property(e => e.ContactEmail).HasMaxLength(255);
                entity.Property(e => e.FacebookUrl).HasMaxLength(500);
                entity.Property(e => e.InstagramUrl).HasMaxLength(500);
                entity.Property(e => e.TwitterUrl).HasMaxLength(500);
                entity.Property(e => e.YouTubeUrl).HasMaxLength(500);
            });

            // Seed default roles
            SeedRoles(modelBuilder);
        }

        private void SeedRoles(ModelBuilder modelBuilder)
        {
            var roles = new List<ApplicationRole>
            {
                new ApplicationRole
                {
                    Id = "1",
                    Name = "Admin",
                    NormalizedName = "ADMIN",
                    Description = "مدير النظام",
                    CreatedAt = DateTime.Now
                },
                new ApplicationRole
                {
                    Id = "2",
                    Name = "Seller",
                    NormalizedName = "SELLER",
                    Description = "بائع/صاحب متجر",
                    CreatedAt = DateTime.Now
                },
                new ApplicationRole
                {
                    Id = "3",
                    Name = "Buyer",
                    NormalizedName = "BUYER",
                    Description = "مشتري/عميل",
                    CreatedAt = DateTime.Now
                }
            };

            modelBuilder.Entity<ApplicationRole>().HasData(roles);

            // Seed admin user
            var adminUser = new ApplicationUser
            {
                Id = "admin-id-12345",
                UserName = "+9647700227210",
                NormalizedUserName = "+9647700227210",
                Email = "admin@reversemarket.iq",
                NormalizedEmail = "ADMIN@REVERSEMARKET.IQ",
                EmailConfirmed = true,
                PhoneNumber = "+9647700227210",
                PhoneNumberConfirmed = true,
                FirstName = "مدير",
                LastName = "النظام",
                DateOfBirth = new DateTime(1990, 1, 1),
                Gender = "ذكر",
                City = "بغداد",
                District = "الكرادة",
                UserType = UserType.Buyer, // Admin doesn't need specific user type
                IsPhoneVerified = true,
                IsEmailVerified = true,
                IsActive = true,
                CreatedAt = DateTime.Now,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };

            // Set password hash for admin (password: Admin@123)
            adminUser.PasswordHash = "AQAAAAIAAYagAAAAEJ3pAOTg7kfNrOQi3F9x8w0iLK1J5AudCF5pN7t8oEj5oMy8q4nRuGHu8C7c0X9Y+Q==";

            modelBuilder.Entity<ApplicationUser>().HasData(adminUser);

            // Assign admin role to admin user
            modelBuilder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string>
                {
                    UserId = adminUser.Id,
                    RoleId = "1" // Admin role
                }
            );
        }
    }
}