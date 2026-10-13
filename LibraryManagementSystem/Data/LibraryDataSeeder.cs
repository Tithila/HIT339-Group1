using LibraryManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Data;

public static class LibraryDataSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // -------------------------
        // Seed Books
        // -------------------------

        if (!await context.Items.AnyAsync(i => i.LibraryCode == "BK001"))
        {
            context.Books.Add(new Book
            {
                Name = "The Hobbit",
                Description = "Fantasy novel",
                LibraryCode = "BK001",
                Status = ItemStatus.Available,
                Author = "J.R.R. Tolkien",
                Genre = "Fantasy"
            });
        }

        if (!await context.Items.AnyAsync(i => i.LibraryCode == "BK002"))
        {
            context.Books.Add(new Book
            {
                Name = "1984",
                Description = "Dystopian novel",
                LibraryCode = "BK002",
                Status = ItemStatus.Available,
                Author = "George Orwell",
                Genre = "Fiction"
            });
        }

        // -------------------------
        // Seed Music
        // -------------------------

        if (!await context.Items.AnyAsync(i => i.LibraryCode == "MU001"))
        {
            context.MusicItems.Add(new Music
            {
                Name = "Thriller",
                Description = "Music album",
                LibraryCode = "MU001",
                Status = ItemStatus.Available,
                Artist = "Michael Jackson",
                Year = 1982
            });
        }

        if (!await context.Items.AnyAsync(i => i.LibraryCode == "MU002"))
        {
            context.MusicItems.Add(new Music
            {
                Name = "25",
                Description = "Music album",
                LibraryCode = "MU002",
                Status = ItemStatus.Available,
                Artist = "Adele",
                Year = 2015
            });
        }

        // -------------------------
        // Seed Toys
        // -------------------------

        if (!await context.Items.AnyAsync(i => i.LibraryCode == "TY001"))
        {
            context.Toys.Add(new Toy
            {
                Name = "Lego Set",
                Description = "Building toy",
                LibraryCode = "TY001",
                Status = ItemStatus.Available,
                Type = "Construction",
                RecommendedAge = 8
            });
        }

        if (!await context.Items.AnyAsync(i => i.LibraryCode == "TY002"))
        {
            context.Toys.Add(new Toy
            {
                Name = "Chess Set",
                Description = "Strategy board game",
                LibraryCode = "TY002",
                Status = ItemStatus.Available,
                Type = "Board Game",
                RecommendedAge = 10
            });
        }

        // -------------------------
        // Seed Borrowers
        // -------------------------

        if (!await context.Borrowers.AnyAsync(b => b.Email == "sarah@example.com"))
        {
            context.Borrowers.Add(new Borrower
            {
                FullName = "Sarah Lee",
                Email = "sarah@example.com",
                Phone = "0411111111",
                IsSuspended = false
            });
        }

        if (!await context.Borrowers.AnyAsync(b => b.Email == "david@example.com"))
        {
            context.Borrowers.Add(new Borrower
            {
                FullName = "David Brown",
                Email = "david@example.com",
                Phone = "0422222222",
                IsSuspended = false
            });
        }

        // -------------------------
        // Seed Fine Policies
        // -------------------------

        if (!await context.FinePolicies.AnyAsync(p => p.ItemType == "Book"))
        {
            context.FinePolicies.Add(new FinePolicy
            {
                ItemType = "Book",
                DailyRate = 0.50m,
                GracePeriodDays = 2,
                MaxFine = 20m
            });
        }

        if (!await context.FinePolicies.AnyAsync(p => p.ItemType == "Music"))
        {
            context.FinePolicies.Add(new FinePolicy
            {
                ItemType = "Music",
                DailyRate = 1.00m,
                GracePeriodDays = 1,
                MaxFine = 25m
            });
        }

        if (!await context.FinePolicies.AnyAsync(p => p.ItemType == "Toy"))
        {
            context.FinePolicies.Add(new FinePolicy
            {
                ItemType = "Toy",
                DailyRate = 1.50m,
                GracePeriodDays = 0,
                MaxFine = 30m
            });
        }

        await context.SaveChangesAsync();
    }
}