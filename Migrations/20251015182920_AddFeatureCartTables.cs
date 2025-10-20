using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EBookDashboard.Migrations
{
    /// <inheritdoc />
    public partial class AddFeatureCartTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Features",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Key = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Price = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Features", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TemporaryFeatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SessionId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FeatureId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AddedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemporaryFeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemporaryFeatures_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserFeatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FeatureId = table.Column<int>(type: "int", nullable: false),
                    AuthorPlanId = table.Column<int>(type: "int", nullable: true),
                    AddedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserFeatures_AuthorPlans_AuthorPlanId",
                        column: x => x.AuthorPlanId,
                        principalTable: "AuthorPlans",
                        principalColumn: "AuthorPlanId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserFeatures_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Features",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "Key", "Name", "Price", "Type" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 10, 15, 18, 29, 18, 466, DateTimeKind.Utc).AddTicks(470), "Create and format professional eBooks with our easy-to-use editor.", true, "ebook_creation", "EBook Creation", 0.00m, "Basic" },
                    { 2, new DateTime(2025, 10, 15, 18, 29, 18, 466, DateTimeKind.Utc).AddTicks(3004), "Design stunning book covers with our AI-powered tool.", true, "cover_design", "Cover Design", 19.99m, "Premium" },
                    { 3, new DateTime(2025, 10, 15, 18, 29, 18, 466, DateTimeKind.Utc).AddTicks(3011), "Convert your book into a professional audiobook.", true, "audio_book", "Audio Book", 49.99m, "Premium" },
                    { 4, new DateTime(2025, 10, 15, 18, 29, 18, 466, DateTimeKind.Utc).AddTicks(3012), "Professional proofreading and editing services.", true, "proofreading", "Proofreading", 29.99m, "Premium" },
                    { 5, new DateTime(2025, 10, 15, 18, 29, 18, 466, DateTimeKind.Utc).AddTicks(3013), "Track your sales and reader engagement with detailed analytics.", true, "analytics", "Analytics", 14.99m, "Premium" },
                    { 6, new DateTime(2025, 10, 15, 18, 29, 18, 466, DateTimeKind.Utc).AddTicks(3017), "Promote your book with our integrated marketing suite.", true, "marketing_tools", "Marketing Tools", 24.99m, "Marketing" }
                });

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "PlanId",
                keyValue: 1,
                column: "CreateddAt",
                value: new DateTime(2025, 10, 15, 18, 29, 18, 464, DateTimeKind.Utc).AddTicks(9528));

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "PlanId",
                keyValue: 2,
                column: "CreateddAt",
                value: new DateTime(2025, 10, 15, 18, 29, 18, 465, DateTimeKind.Utc).AddTicks(1011));

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "PlanId",
                keyValue: 3,
                column: "CreateddAt",
                value: new DateTime(2025, 10, 15, 18, 29, 18, 465, DateTimeKind.Utc).AddTicks(1107));

            migrationBuilder.InsertData(
                table: "Plans",
                columns: new[] { "PlanId", "AllowAnalytics", "AllowDownloads", "AllowFullDashboard", "AllowPublishing", "CreateddAt", "IsActive", "MaxChapters", "MaxEBooks", "MaxPages", "PlanDays", "PlanDescription", "PlanHours", "PlanName", "PlanRate" },
                values: new object[] { 4, false, false, false, false, new DateTime(2025, 10, 15, 18, 29, 18, 465, DateTimeKind.Utc).AddTicks(1108), false, 0, 0, 0, 365, "Premium yearly subscription with all features", 0, "Premium Plan", 199.99m });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleId", "AllowAnalytics", "AllowDelete", "AllowDownloads", "AllowEdit", "AllowFullDashboard", "AllowPublishing", "Description", "RoleName" },
                values: new object[,]
                {
                    { 1, true, true, true, true, true, true, "Administrator with full access", "Admin" },
                    { 2, true, true, true, true, true, true, "Author with publishing access", "Author" },
                    { 3, false, false, false, false, false, false, "Reader with limited access", "Reader" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TemporaryFeatures_FeatureId",
                table: "TemporaryFeatures",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFeatures_AuthorPlanId",
                table: "UserFeatures",
                column: "AuthorPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_UserFeatures_FeatureId",
                table: "UserFeatures",
                column: "FeatureId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TemporaryFeatures");

            migrationBuilder.DropTable(
                name: "UserFeatures");

            migrationBuilder.DropTable(
                name: "Features");

            migrationBuilder.DeleteData(
                table: "Plans",
                keyColumn: "PlanId",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3);

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "PlanId",
                keyValue: 1,
                column: "CreateddAt",
                value: new DateTime(2025, 9, 30, 20, 7, 30, 280, DateTimeKind.Utc).AddTicks(4863));

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "PlanId",
                keyValue: 2,
                column: "CreateddAt",
                value: new DateTime(2025, 9, 30, 20, 7, 30, 280, DateTimeKind.Utc).AddTicks(6351));

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "PlanId",
                keyValue: 3,
                column: "CreateddAt",
                value: new DateTime(2025, 9, 30, 20, 7, 30, 281, DateTimeKind.Utc).AddTicks(413));
        }
    }
}
