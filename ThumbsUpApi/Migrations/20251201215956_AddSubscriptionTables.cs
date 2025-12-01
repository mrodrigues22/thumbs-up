using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThumbsUpApi.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaddleCustomerId",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SubscriptionId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubscriptionTier",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    PaddleSubscriptionId = table.Column<string>(type: "text", nullable: true),
                    PaddleCustomerId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    PaddlePriceId = table.Column<string>(type: "text", nullable: true),
                    PlanName = table.Column<string>(type: "text", nullable: true),
                    CurrentPeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CurrentPeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PausedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TrialEndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaddleTransactionId = table.Column<string>(type: "text", nullable: false),
                    PaddleSubscriptionId = table.Column<string>(type: "text", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Details = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Transactions_Subscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "Subscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PaddleSubscriptionId",
                table: "Subscriptions",
                column: "PaddleSubscriptionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_Status",
                table: "Subscriptions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_UserId",
                table: "Subscriptions",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_CreatedAt",
                table: "Transactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_PaddleTransactionId",
                table: "Transactions",
                column: "PaddleTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_SubscriptionId",
                table: "Transactions",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_UserId",
                table: "Transactions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "PaddleCustomerId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SubscriptionId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SubscriptionTier",
                table: "AspNetUsers");
        }
    }
}
