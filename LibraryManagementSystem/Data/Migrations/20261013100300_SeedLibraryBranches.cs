
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryManagementSystem.Data.Migrations
{
    public partial class SeedLibraryBranches : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insert three demonstration library branches.
            migrationBuilder.InsertData(
                table: "Branches",
                columns: new[]
                {
                    "Name",
                    "Code",
                    "Address",
                    "OpeningHours"
                },
                values: new object[,]
                {
                    {
                        "Central Library",
                        "BR001",
                        "100 Main Street",
                        "Mon-Fri 9:00 AM - 6:00 PM"
                    },
                    {
                        "North Branch",
                        "BR002",
                        "25 North Road",
                        "Mon-Sat 9:00 AM - 5:00 PM"
                    },
                    {
                        "South Branch",
                        "BR003",
                        "50 South Avenue",
                        "Mon-Fri 10:00 AM - 6:00 PM"
                    }
                });

            // Assign existing items without a branch to Central Library.
            migrationBuilder.Sql("""
                UPDATE Items
                SET BranchId = (
                    SELECT Id
                    FROM Branches
                    WHERE Code = 'BR001'
                )
                WHERE BranchId IS NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Clear assignments before removing demonstration branches.
            migrationBuilder.Sql("""
                UPDATE Items
                SET BranchId = NULL
                WHERE BranchId IN (
                    SELECT Id
                    FROM Branches
                    WHERE Code IN ('BR001', 'BR002', 'BR003')
                );
                """);

            migrationBuilder.Sql("""
                DELETE FROM Branches
                WHERE Code IN ('BR001', 'BR002', 'BR003');
                """);
        }
    }
}
