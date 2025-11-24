using Microsoft.EntityFrameworkCore;
using ErpConsoleApp.Database.Models;

namespace ErpConsoleApp.Database
{
    /// <summary>
    // This is the "brain" that connects our C# classes to the SQLite database.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public DbSet<Party> Parties { get; set; }
        public DbSet<PurchaseSlip> PurchaseSlips { get; set; }

        public DbSet<Item> Items { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<SalaryRecord> Salaries { get; set; }
        public DbSet<Voucher> Vouchers { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<AppSetting> Settings { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // This creates a file named "erp.db" in the same folder as the .exe
            // --- MODIFIED LINE ---
            // Go up 3 directories (from bin/Debug/net8.0) to the project root
            optionsBuilder.UseSqlite("Data Source=../../../erp.db");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Seed default PIN
            modelBuilder.Entity<AppSetting>().HasData(
                new AppSetting { Key = "LoginPin", Value = "1234" }
            );
        }
    }
}