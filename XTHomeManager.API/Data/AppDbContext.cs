using Microsoft.EntityFrameworkCore;
using XTHomeManager.API.Models;

namespace XTHomeManager.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Record> Records { get; set; }
        public DbSet<MilkEntry> MilkEntries { get; set; }
        public DbSet<ElectricityBill> ElectricityBills { get; set; }
        public DbSet<RentEntry> RentEntries { get; set; }
        public DbSet<Settings> Settings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User configuration
            modelBuilder.Entity<User>().Property(u => u.Id).HasMaxLength(450);
            modelBuilder.Entity<User>().Property(u => u.Email).IsRequired().HasMaxLength(450);
            modelBuilder.Entity<User>().Property(u => u.FullName).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<User>().Property(u => u.PasswordHash).IsRequired();
            modelBuilder.Entity<User>().Property(u => u.Role).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<User>().Property(u => u.AdminId).IsRequired(false).HasMaxLength(450);
            modelBuilder.Entity<User>().Property(u => u.ImagePath).IsRequired(false);

            // Record configuration
            modelBuilder.Entity<Record>().Property(r => r.Name).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<Record>().Property(r => r.Type).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<Record>().Property(r => r.UserId).IsRequired().HasMaxLength(450);
            modelBuilder.Entity<Record>().Property(r => r.ViewerId).IsRequired(false).HasMaxLength(450);

            // MilkEntry configuration
            modelBuilder.Entity<MilkEntry>().Property(m => m.Status).IsRequired(false).HasMaxLength(50);
            modelBuilder.Entity<MilkEntry>().Property(m => m.Date).IsRequired();
            modelBuilder.Entity<MilkEntry>().Property(m => m.QuantityLiters).IsRequired().HasColumnType("decimal(18,2)");
            modelBuilder.Entity<MilkEntry>().Property(m => m.TotalCost).IsRequired().HasColumnType("decimal(18,2)");
            modelBuilder.Entity<MilkEntry>().Property(m => m.AdminId).IsRequired().HasMaxLength(450);

            // ElectricityBill configuration
            modelBuilder.Entity<ElectricityBill>().Property(b => b.ReferenceNumber).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<ElectricityBill>().Property(b => b.Month).IsRequired().HasMaxLength(7);
            modelBuilder.Entity<ElectricityBill>().Property(b => b.FilePath).IsRequired(false).HasMaxLength(500);
            modelBuilder.Entity<ElectricityBill>().Property(b => b.Amount).IsRequired().HasColumnType("decimal(18,2)");
            modelBuilder.Entity<ElectricityBill>().Property(b => b.AdminId).IsRequired().HasMaxLength(450);

            // RentEntry configuration
            modelBuilder.Entity<RentEntry>().Property(r => r.Month).IsRequired().HasMaxLength(7);
            modelBuilder.Entity<RentEntry>().Property(r => r.Amount).IsRequired().HasColumnType("decimal(18,2)");
            modelBuilder.Entity<RentEntry>().Property(r => r.AdminId).IsRequired().HasMaxLength(450);

            // Settings configuration
            modelBuilder.Entity<Settings>().Property(s => s.MilkRatePerLiter).IsRequired().HasColumnType("decimal(18,2)");
            modelBuilder.Entity<Settings>().Property(s => s.UserId).IsRequired().HasMaxLength(450);
        }
    }
}