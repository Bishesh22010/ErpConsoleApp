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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // This creates a file named "erp.db" in the same folder as the .exe
            optionsBuilder.UseSqlite("Data Source=erp.db");
        }
    }
}