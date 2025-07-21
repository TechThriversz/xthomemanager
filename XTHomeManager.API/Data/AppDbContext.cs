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
            modelBuilder.Entity<Record>().Property(r => r.Name).HasMaxLength(100);
            modelBuilder.Entity<Record>().Property(r => r.Type).HasMaxLength(50);
            modelBuilder.Entity<User>().Property(u => u.FullName).HasMaxLength(100);
            modelBuilder.Entity<MilkEntry>().Property(m => m.QuantityLiters).HasPrecision(18, 2);
            modelBuilder.Entity<MilkEntry>().Property(m => m.RatePerLiter).HasPrecision(18, 2);
            modelBuilder.Entity<MilkEntry>().Property(m => m.Status).HasMaxLength(50);
            modelBuilder.Entity<ElectricityBill>().Property(b => b.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<ElectricityBill>().Property(b => b.ReferenceNumber).HasMaxLength(100);
            modelBuilder.Entity<ElectricityBill>().Property(b => b.FilePath).HasMaxLength(500);
            modelBuilder.Entity<ElectricityBill>().Property(b => b.Month).HasMaxLength(7);
            modelBuilder.Entity<RentEntry>().Property(r => r.Amount).HasPrecision(18, 2);
            modelBuilder.Entity<RentEntry>().Property(r => r.Month).HasMaxLength(7);
            modelBuilder.Entity<Settings>().Property(s => s.MilkRatePerLiter).HasPrecision(18, 2);
        }
    }
}