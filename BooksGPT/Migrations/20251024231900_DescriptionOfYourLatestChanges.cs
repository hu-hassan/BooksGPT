using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BooksGPT.Migrations
{
    /// <inheritdoc />
    public partial class DescriptionOfYourLatestChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "author",
                table: "ChatHistory",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "author",
                table: "ChatHistory");
        }
    }
}
