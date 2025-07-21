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
            modelBuilder.Entity<Record>().Property(r => r.Id).HasMaxLength(450);
            modelBuilder.Entity<Record>().Property(r => r.Name).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<Record>().Property(r => r.Type).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<Record>().Property(r => r.UserId).IsRequired().HasMaxLength(450);
            modelBuilder.Entity<Record>().Property(r => r.ViewerId).IsRequired(false).HasMaxLength(450);

            // Milk configuration
            modelBuilder.Entity<MilkEntry>().Property(m => m.Id).HasMaxLength(450);
            modelBuilder.Entity<MilkEntry>().Property(m => m.RecordId).IsRequired().HasMaxLength(450);
            modelBuilder.Entity<MilkEntry>().Property(m => m.Status).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<MilkEntry>().Property(m => m.Date).IsRequired();

            // ElectricityBill configuration
            modelBuilder.Entity<ElectricityBill>().Property(b => b.Id).HasMaxLength(450);
            modelBuilder.Entity<ElectricityBill>().Property(b => b.RecordId).IsRequired().HasMaxLength(450);
            modelBuilder.Entity<ElectricityBill>().Property(b => b.ReferenceNumber).IsRequired().HasMaxLength(100);
            modelBuilder.Entity<ElectricityBill>().Property(b => b.Month).IsRequired();
            modelBuilder.Entity<ElectricityBill>().Property(b => b.FilePath).IsRequired(false);

            // RentEntry configuration
            modelBuilder.Entity<RentEntry>().Property(r => r.Id).HasMaxLength(450);
            modelBuilder.Entity<RentEntry>().Property(r => r.RecordId).IsRequired().HasMaxLength(450);
            modelBuilder.Entity<RentEntry>().Property(r => r.Month).IsRequired();

            // Setting configuration
            modelBuilder.Entity<Settings>().Property(s => s.Id).HasMaxLength(450);
            modelBuilder.Entity<Settings>().Property(s => s.MilkRatePerLiter).IsRequired();
        }
    }
}