using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sayiad.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSubscriptionPricesToEGP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Tier",
                keyValue: 0,
                columns: new[] { "Currency" },
                values: new object[] { "EGP" });

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Tier",
                keyValue: 1,
                columns: new[] { "Price", "Currency" },
                values: new object[] { 500m, "EGP" });

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Tier",
                keyValue: 2,
                columns: new[] { "Price", "Currency" },
                values: new object[] { 1000m, "EGP" });

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Tier",
                keyValue: 3,
                columns: new[] { "Price", "Currency" },
                values: new object[] { 2500m, "EGP" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Tier",
                keyValue: 0,
                columns: new[] { "Currency" },
                values: new object[] { "USD" });

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Tier",
                keyValue: 1,
                columns: new[] { "Price", "Currency" },
                values: new object[] { 10m, "USD" });

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Tier",
                keyValue: 2,
                columns: new[] { "Price", "Currency" },
                values: new object[] { 20m, "USD" });

            migrationBuilder.UpdateData(
                table: "SubscriptionPlans",
                keyColumn: "Tier",
                keyValue: 3,
                columns: new[] { "Price", "Currency" },
                values: new object[] { 50m, "USD" });
        }
    }
}
