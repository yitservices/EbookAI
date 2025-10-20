using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EBookDashboard.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFeatureCartTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 15, 18, 33, 5, 343, DateTimeKind.Utc).AddTicks(2253));

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 15, 18, 33, 5, 343, DateTimeKind.Utc).AddTicks(3952));

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 15, 18, 33, 5, 343, DateTimeKind.Utc).AddTicks(3957));

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 15, 18, 33, 5, 343, DateTimeKind.Utc).AddTicks(3959));

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 15, 18, 33, 5, 343, DateTimeKind.Utc).AddTicks(3960));

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 15, 18, 33, 5, 343, DateTimeKind.Utc).AddTicks(3962));

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "PlanId",
                keyValue: 1,
                column: "CreateddAt",
                value: new DateTime(2025, 10, 15, 18, 33, 5, 342, DateTimeKind.Utc).AddTicks(1013));

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "PlanId",
                keyValue: 2,
                column: "CreateddAt",
                value: new DateTime(2025, 10, 15, 18, 33, 5, 342, DateTimeKind.Utc).AddTicks(2499));

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "PlanId",
                keyValue: 3,
                column: "CreateddAt",
                value: new DateTime(2025, 10, 15, 18, 33, 5, 342, DateTimeKind.Utc).AddTicks(2596));

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "PlanId",
                keyValue: 4,
                column: "CreateddAt",
                value: new DateTime(2025, 10, 15, 18, 33, 5, 342, DateTimeKind.Utc).AddTicks(2598));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 15, 18, 29, 18, 466, DateTimeKind.Utc).AddTicks(470));

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 15, 18, 29, 18, 466, DateTimeKind.Utc).AddTicks(3004));

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 15, 18, 29, 18, 466, DateTimeKind.Utc).AddTicks(3011));

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 15, 18, 29, 18, 466, DateTimeKind.Utc).AddTicks(3012));

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 15, 18, 29, 18, 466, DateTimeKind.Utc).AddTicks(3013));

            migrationBuilder.UpdateData(
                table: "Features",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 15, 18, 29, 18, 466, DateTimeKind.Utc).AddTicks(3017));

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

            migrationBuilder.UpdateData(
                table: "Plans",
                keyColumn: "PlanId",
                keyValue: 4,
                column: "CreateddAt",
                value: new DateTime(2025, 10, 15, 18, 29, 18, 465, DateTimeKind.Utc).AddTicks(1108));
        }
    }
}
