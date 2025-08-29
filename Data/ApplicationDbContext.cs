using Microsoft.EntityFrameworkCore;
using ReverseMarket.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace ReverseMarket.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
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

            // User configurations
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20).IsRequired();
                entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Gender).HasMaxLength(10).IsRequired();
                entity.Property(e => e.City).HasMaxLength(100).IsRequired();
                entity.Property(e => e.District).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Location).HasMaxLength(255);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.StoreName).HasMaxLength(255);
                entity.Property(e => e.StoreDescription).HasMaxLength(1000);

                entity.HasIndex(e => e.PhoneNumber).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Category configurations
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.ImagePath).HasMaxLength(500); // إضافة دعم مسار الصورة

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

                entity.HasOne(e => e.User)
                    .WithMany(e => e.Requests)
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

                entity.HasOne(e => e.User)
                    .WithMany(e => e.StoreCategories)
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
        }
    }
}
