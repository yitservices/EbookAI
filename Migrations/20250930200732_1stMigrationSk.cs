using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EBookDashboard.Migrations
{
    /// <inheritdoc />
    public partial class _1stMigrationSk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Plans",
                columns: new[] { "PlanId", "AllowAnalytics", "AllowDownloads", "AllowFullDashboard", "AllowPublishing", "CreateddAt", "IsActive", "MaxChapters", "MaxEBooks", "MaxPages", "PlanDays", "PlanDescription", "PlanHours", "PlanName", "PlanRate" },
                values: new object[,]
                {
                    { 1, false, false, false, false, new DateTime(2025, 9, 30, 20, 7, 30, 280, DateTimeKind.Utc).AddTicks(4863), false, 0, 0, 0, 30, "Free 1-month trial", 0, "Free Trial", 0m },
                    { 2, false, false, false, false, new DateTime(2025, 9, 30, 20, 7, 30, 280, DateTimeKind.Utc).AddTicks(6351), false, 0, 0, 0, 30, "Basic monthly subscription", 0, "Basic Plan", 9.99m },
                    { 3, false, false, false, false, new DateTime(2025, 9, 30, 20, 7, 30, 281, DateTimeKind.Utc).AddTicks(413), false, 0, 0, 0, 365, "Yearly subscription", 0, "Pro Plan", 99.99m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthorPlans_PlanId",
                table: "AuthorPlans",
                column: "PlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_AuthorPlans_Plans_PlanId",
                table: "AuthorPlans",
                column: "PlanId",
                principalTable: "Plans",
                principalColumn: "PlanId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuthorPlans_Plans_PlanId",
                table: "AuthorPlans");

            migrationBuilder.DropIndex(
                name: "IX_AuthorPlans_PlanId",
                table: "AuthorPlans");

            migrationBuilder.DeleteData(
                table: "Plans",
                keyColumn: "PlanId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Plans",
                keyColumn: "PlanId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Plans",
                keyColumn: "PlanId",
                keyValue: 3);
        }
    }
}
