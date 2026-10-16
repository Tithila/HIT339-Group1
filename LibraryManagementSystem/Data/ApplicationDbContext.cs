
using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Data
{
    public class ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext(options)
    {
        // Existing Library Management System
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

        // Anjana: Multi-Branch Inventory
        public DbSet<Branch> Branches { get; set; }

        // Anjana: Inter-Branch Transfers
        public DbSet<TransferRequest> TransferRequests { get; set; }

        // Shereena: Automated Notifications
        public DbSet<NotificationLog> NotificationLogs { get; set; }

        // Shereena: Reservation and Waitlist
        public DbSet<Reservation> Reservations { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Unique library item codes
            builder.Entity<Item>()
                .HasIndex(i => i.LibraryCode)
                .IsUnique();

            // Unique borrower card numbers
            builder.Entity<Borrower>()
                .HasIndex(b => b.LibraryCardNumber)
                .IsUnique()
                .HasFilter("[LibraryCardNumber] IS NOT NULL");

            // Anjana: Unique branch codes
            builder.Entity<Branch>()
                .HasIndex(b => b.Code)
                .IsUnique();

            // Anjana: Item and Branch relationship
            builder.Entity<Item>()
                .HasOne(i => i.Branch)
                .WithMany(b => b.Items)
                .HasForeignKey(i => i.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            // Anjana: Transfer relationships
            builder.Entity<TransferRequest>(entity =>
            {
                entity.HasOne(t => t.Item)
                    .WithMany()
                    .HasForeignKey(t => t.ItemId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.FromBranch)
                    .WithMany()
                    .HasForeignKey(t => t.FromBranchId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(t => t.ToBranch)
                    .WithMany()
                    .HasForeignKey(t => t.ToBranchId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(t => t.Status);
                entity.HasIndex(t => t.ItemId);
            });

            // Shereena: Reservation and Item relationship
            builder.Entity<Reservation>()
                .HasOne(r => r.Item)
                .WithMany()
                .HasForeignKey(r => r.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            // Shereena: Reservation and Borrower relationship
            builder.Entity<Reservation>()
                .HasOne(r => r.Borrower)
                .WithMany()
                .HasForeignKey(r => r.BorrowerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Shereena: Waitlist lookup index
            builder.Entity<Reservation>()
                .HasIndex(r => new
                {
                    r.ItemId,
                    r.Status,
                    r.RequestedDate
                });

            // Shereena: Notification and Borrower relationship
            builder.Entity<NotificationLog>()
                .HasOne(n => n.Borrower)
                .WithMany()
                .HasForeignKey(n => n.BorrowerId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
