using Microsoft.EntityFrameworkCore;
using EBookDashboard.Models;

namespace EBookDashboard.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        // 📚 Book-related
        public DbSet<Books> Books { get; set; }
        public DbSet<BookPrice> BookPrices { get; set; }
        public DbSet<BookVersion> BookVersions { get; set; }
        public DbSet<Chapters> Chapters { get; set; }

        // Add this new DbSet for raw responses
        public DbSet<APIRawResponse> APIRawResponse { get; set; }
        public DbSet<FinalizeChapters> FinalizeChapters { get; set; }
       
        // ✍️ Author-related
        public DbSet<Authors> Authors { get; set; }
        public DbSet<AuthorPlans> AuthorPlans { get; set; }
        public DbSet<AuthorPlanFeatures> AuthorPlanFeaturesSet { get; set; } // Renamed to avoid conflict
        public DbSet<AuthorBills> AuthorBills { get; set; }

        // 👥 User-related
        public DbSet<Users> Users { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        // UserPreferences and UserFeatures removed - using localStorage and AuthorPlans instead
        public DbSet<TemporaryFeatures> TemporaryFeatures { get; set; }
        public DbSet<Features> Features { get; set; }

        // 🏷 Misc / others
        public DbSet<Categories> Categories { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<Plans> Plans { get; set; }
        public DbSet<PlanFeatures> PlanFeatures { get; set; }
        public DbSet<PubCost> PubCosts { get; set; }
        public DbSet<RecordStatus> RecordStatus { get; set; }
        public DbSet<Settings> Settings { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public object AuthorPlanFeatures { get; internal set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Plans>().HasData(
                new Plans { PlanId = 1, PlanName = "Free Trial", PlanRate = 0, PlanDays = 30, PlanDescription = "Free 1-month trial" },
                new Plans { PlanId = 2, PlanName = "Basic Plan", PlanRate = 9.99m, PlanDays = 30, PlanDescription = "Basic monthly subscription" },
                new Plans { PlanId = 3, PlanName = "Pro Plan", PlanRate = 99.99m, PlanDays = 365, PlanDescription = "Yearly subscription" });

            // Seed Roles data
            modelBuilder.Entity<Roles>().HasData(
                new Roles { RoleId = 1, RoleName = "Admin", Description = "Administrator with full access", AllowDownloads = true, AllowFullDashboard = true, AllowAnalytics = true, AllowPublishing = true, AllowDelete = true, AllowEdit = true },
                new Roles { RoleId = 2, RoleName = "Author", Description = "Author with publishing access", AllowDownloads = true, AllowFullDashboard = true, AllowAnalytics = true, AllowPublishing = true, AllowDelete = true, AllowEdit = true },
                new Roles { RoleId = 3, RoleName = "Reader", Description = "Reader with limited access", AllowDownloads = false, AllowFullDashboard = false, AllowAnalytics = false, AllowPublishing = false, AllowDelete = false, AllowEdit = false });

            // ✅ Decimal precision for MySQL
            modelBuilder.Entity<BookPrice>()
                .Property(b => b.bookPrice)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Plans>()
                .Property(p => p.PlanRate)
                .HasPrecision(10, 2);

            modelBuilder.Entity<PubCost>()
                .Property(p => p.Amount)
                .HasPrecision(10, 2);
                
            modelBuilder.Entity<PlanFeatures>()
                .Property(p => p.FeatureRate)
                .HasPrecision(10, 2);
                
            modelBuilder.Entity<AuthorPlanFeatures>()
                .Property(p => p.FeatureRate)
                .HasPrecision(10, 2);
                
            modelBuilder.Entity<AuthorPlanFeatures>()
                .Property(p => p.TotalAmount)
                .HasPrecision(10, 2);

            // ✅ Example: Unique constraint on AuthorCode
            modelBuilder.Entity<Authors>()
                .HasIndex(a => a.AuthorCode)
                .IsUnique();

            // ✅ Example: Relationships (optional, add as needed)
            modelBuilder.Entity<Books>()
                .HasOne<Authors>()
                .WithMany(a => a.Books)
                .HasForeignKey(b => b.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Chapters>()
                .HasOne<Books>()
                .WithMany(b => b.Chapters)
                .HasForeignKey(c => c.BookId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ensure Users table has a foreign key relationship to Roles
            modelBuilder.Entity<Users>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure relationships for feature tables
            modelBuilder.Entity<AuthorPlanFeatures>()
                .HasOne(apf => apf.PlanFeature)
                .WithMany()
                .HasForeignKey(apf => apf.FeatureId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserFeatures relationship removed - using AuthorPlans -> AuthorPlanFeatures -> Features instead

            // Configure TemporaryFeatures to Features relationship
            modelBuilder.Entity<TemporaryFeatures>()
                .HasOne(tf => tf.Feature)
                .WithMany()
                .HasForeignKey(tf => tf.FeatureId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ensure relationship between AuthorPlanFeatures and AuthorBills via BillId
            modelBuilder.Entity<AuthorPlanFeatures>()
                .HasOne(apf => apf.AuthorBill)
                .WithMany(b => b.AuthorPlanFeatures)
                .HasForeignKey(apf => apf.BillId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Notification relationships
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // UserPreference relationship removed - using localStorage for preferences

            // Configure AuditLog relationships
            modelBuilder.Entity<AuditLog>()
                .HasOne(al => al.User)
                .WithMany()
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Unique constraint on Settings Key
            modelBuilder.Entity<Settings>()
                .HasIndex(s => s.Key)
                .IsUnique();
        }
    }
}