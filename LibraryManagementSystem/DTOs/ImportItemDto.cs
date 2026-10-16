using System;

namespace LibraryManagementSystem.DTOs
{
    public class ImportItemDto
    {
        // Row index from CSV (1-based)
        public int RowNumber { get; set; }

        public string? ItemType { get; set; } // Book, Music, Toy
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? LibraryCode { get; set; }
        public string? BranchCode { get; set; }

        // Book specific
        public string? Author { get; set; }
        public string? Genre { get; set; }

        // Music specific
        public string? Artist { get; set; }
        public int? Year { get; set; }

        // Toy specific
        public string? Type { get; set; }
        public int? RecommendedAge { get; set; }

        // Optional initial status (Available/Loaned/Damaged)
        public string? Status { get; set; }
    }
}
