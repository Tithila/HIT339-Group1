using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using CsvHelper;
using LibraryManagementSystem.DTOs;

namespace LibraryManagementSystem.Services.CsvMaps;

public sealed class ImportItemDtoMap : ClassMap<ImportItemDto>
{
    public ImportItemDtoMap()
    {
        Map(m => m.ItemType).Name("ItemType", "Item Type", "type");
        Map(m => m.Name).Name("Name", "Title");
        Map(m => m.Description).Name("Description", "Desc");
        Map(m => m.LibraryCode).Name("LibraryCode", "Library Code", "library_code");
        Map(m => m.BranchCode).Name("BranchCode", "Branch", "Branch Code");
        Map(m => m.Author).Name("Author");
        Map(m => m.Genre).Name("Genre");
        Map(m => m.Artist).Name("Artist");
        Map(m => m.Year).Name("Year").TypeConverter<NullableInt32Converter>();
        Map(m => m.Type).Name("Type");
        Map(m => m.RecommendedAge).Name("RecommendedAge", "Recommended Age").TypeConverter<NullableInt32Converter>();
        Map(m => m.Status).Name("Status");
    }
}

public class NullableInt32Converter : DefaultTypeConverter
{
    public override object? ConvertFromString(string? text, IReaderRow? row, MemberMapData? memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (int.TryParse(text, out var v)) return v;
        return null;
    }
}
