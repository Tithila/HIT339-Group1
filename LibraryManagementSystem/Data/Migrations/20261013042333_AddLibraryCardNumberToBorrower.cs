using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryManagementSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLibraryCardNumberToBorrower : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LibraryCardNumber",
                table: "Borrowers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Borrowers_LibraryCardNumber",
                table: "Borrowers",
                column: "LibraryCardNumber",
                unique: true,
                filter: "[LibraryCardNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Borrowers_LibraryCardNumber",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "LibraryCardNumber",
                table: "Borrowers");
        }
    }
}
