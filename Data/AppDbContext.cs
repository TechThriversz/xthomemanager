using Microsoft.EntityFrameworkCore;
using XTHomeManager.API.Models;

namespace XTHomeManager.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<MilkEntry> MilkEntries { get; set; }
        public DbSet<ElectricityBill> ElectricityBills { get; set; }
        public DbSet<RentEntry> RentEntries { get; set; }
    }
}