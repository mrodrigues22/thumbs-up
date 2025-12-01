using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThumbsUpApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAiFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientSummaries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    SummaryText = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientSummaries_Clients_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Clients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContentFeatures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OcrText = table.Column<string>(type: "text", nullable: true),
                    ThemeTagsJson = table.Column<string>(type: "text", nullable: true),
                    ExtractedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentFeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentFeatures_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClientSummaries_ClientId",
                table: "ClientSummaries",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentFeatures_SubmissionId",
                table: "ContentFeatures",
                column: "SubmissionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClientSummaries");

            migrationBuilder.DropTable(
                name: "ContentFeatures");
        }
    }
}
