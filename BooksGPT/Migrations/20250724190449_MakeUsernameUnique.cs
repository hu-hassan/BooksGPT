using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BooksGPT.Migrations
{
    /// <inheritdoc />
    public partial class MakeUsernameUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE [users] ADD CONSTRAINT [UQ_users_Username] UNIQUE ([Username])");
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_Username",
                table: "users");
        }

    }
}
