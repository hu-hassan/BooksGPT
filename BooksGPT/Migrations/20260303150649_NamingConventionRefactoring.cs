using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BooksGPT.Migrations
{
    /// <inheritdoc />
    public partial class NamingConventionRefactoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "userquestions",
                table: "ChatHistory",
                newName: "UserQuestions");

            migrationBuilder.RenameColumn(
                name: "title",
                table: "ChatHistory",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "isBookSelected",
                table: "ChatHistory",
                newName: "IsBookSelected");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "ChatHistory",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "botanswers",
                table: "ChatHistory",
                newName: "BotAnswers");

            migrationBuilder.RenameColumn(
                name: "author",
                table: "ChatHistory",
                newName: "Author");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserQuestions",
                table: "ChatHistory",
                newName: "userquestions");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "ChatHistory",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "IsBookSelected",
                table: "ChatHistory",
                newName: "isBookSelected");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "ChatHistory",
                newName: "email");

            migrationBuilder.RenameColumn(
                name: "BotAnswers",
                table: "ChatHistory",
                newName: "botanswers");

            migrationBuilder.RenameColumn(
                name: "Author",
                table: "ChatHistory",
                newName: "author");
        }
    }
}
