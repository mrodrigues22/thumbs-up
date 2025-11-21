using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThumbsUpApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAccessPasswordToSubmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessPassword",
                table: "Submissions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessPassword",
                table: "Submissions");
        }
    }
}
