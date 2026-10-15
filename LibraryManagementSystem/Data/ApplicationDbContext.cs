
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Data
{
    public class ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext(options)
    {
        // Existing library management tables
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

        // Inter-branch transfer requests
        public DbSet<TransferRequest> TransferRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Each borrower must have a unique library card number.
            // Borrowers without a card number are permitted.
            builder.Entity<Borrower>()
                .HasIndex(b => b.LibraryCardNumber)
                .IsUnique()
                .HasFilter("[LibraryCardNumber] IS NOT NULL");

            // Each branch must have a unique code.
            builder.Entity<Branch>()
                .HasIndex(b => b.Code)
                .IsUnique();

            // Each library item belongs to one branch.
            // A branch can contain multiple items.
            builder.Entity<Item>()
                .HasOne(i => i.Branch)
                .WithMany(b => b.Items)
                .HasForeignKey(i => i.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            // Library codes must be unique.
            builder.Entity<Item>()
                .HasIndex(i => i.LibraryCode)
                .IsUnique();

            // Inter-branch transfer relationships.
            builder.Entity<TransferRequest>(entity =>
            {
                // The item being transferred.
                entity.HasOne(t => t.Item)
                    .WithMany()
                    .HasForeignKey(t => t.ItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                // The branch sending the item.
                entity.HasOne(t => t.FromBranch)
                    .WithMany()
                    .HasForeignKey(t => t.FromBranchId)
                    .OnDelete(DeleteBehavior.Restrict);

                // The branch receiving the item.
                entity.HasOne(t => t.ToBranch)
                    .WithMany()
                    .HasForeignKey(t => t.ToBranchId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Indexes for transfer searches.
                entity.HasIndex(t => t.Status);
                entity.HasIndex(t => t.ItemId);
            });
        }
    }
}
