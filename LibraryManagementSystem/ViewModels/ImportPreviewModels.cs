using System.Collections.Generic;
using LibraryManagementSystem.DTOs;

namespace LibraryManagementSystem.ViewModels
{
    public class ImportPreviewRow
    {
        public int RowNumber { get; set; }
        public ImportItemDto? Item { get; set; }
        public bool IsValid { get; set; }
        public bool IsDuplicate { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class ImportPreviewViewModel
    {
        public List<ImportPreviewRow> ValidRows { get; set; } = new List<ImportPreviewRow>();
        public List<ImportPreviewRow> InvalidRows { get; set; } = new List<ImportPreviewRow>();
        public List<ImportPreviewRow> Duplicates { get; set; } = new List<ImportPreviewRow>();

        // Summary
        public int TotalRows => ValidRows.Count + InvalidRows.Count + Duplicates.Count;
        public int ValidCount => ValidRows.Count;
        public int InvalidCount => InvalidRows.Count;
        public int DuplicateCount => Duplicates.Count;

        // For anonymized export flag if needed
        public bool Anonymized { get; set; }
    }
}
