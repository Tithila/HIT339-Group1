
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Data
{
    public class ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext(options)
    {
        public DbSet<Item> Items { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<Music> MusicItems { get; set; }
        public DbSet<Toy> Toys { get; set; }
        public DbSet<Borrower> Borrowers { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<FinePolicy> FinePolicies { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<ItemRequest> ItemRequests { get; set; }
        public DbSet<ReturnInspection> ReturnInspections { get; set; }

        // Multi-branch inventory
        public DbSet<Branch> Branches { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Each branch must have a unique code.
            builder.Entity<Branch>()
                .HasIndex(b => b.Code)
                .IsUnique();

            // One branch can hold multiple library items.
            builder.Entity<Item>()
                .HasOne(i => i.Branch)
                .WithMany(b => b.Items)
                .HasForeignKey(i => i.BranchId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Item>()
                .HasIndex(i => i.LibraryCode)
                .IsUnique();

            builder.Entity<Borrower>()
                .HasIndex(b => b.LibraryCardNumber)
                .IsUnique()
                .HasFilter("[LibraryCardNumber] IS NOT NULL");
        }
    }
}
