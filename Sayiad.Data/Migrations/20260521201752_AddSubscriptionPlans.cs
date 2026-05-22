using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sayiad.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tier = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    BillingCycle = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MaxAuctionsPerMonth = table.Column<int>(type: "int", nullable: false),
                    MaxBidsPerMonth = table.Column<int>(type: "int", nullable: false),
                    MaxAuctionRequestsPerMonth = table.Column<int>(type: "int", nullable: false),
                    Features = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Tier",
                table: "SubscriptionPlans",
                column: "Tier",
                unique: true);

            // Seed default plans (CreatedAt has default GETUTCDATE())
            migrationBuilder.InsertData("SubscriptionPlans",
                columns: ["Tier", "Name", "Description", "Price", "Currency", "BillingCycle",
                    "MaxAuctionsPerMonth", "MaxBidsPerMonth", "MaxAuctionRequestsPerMonth",
                    "Features", "IsActive", "SortOrder"],
                values: new object[,]
                {
                    { 0, "Free", "Basic access to browse and explore the marketplace", 0m, "USD", "Monthly", 3, 3, 3, "[\"Browse products and auctions\",\"Place bids and make purchases\"]", true, 1 },
                    { 1, "Basic", "Full access with priority support and extra features", 10m, "USD", "Monthly", 10, 20, 10, "[\"Browse products and auctions\",\"Place bids and make purchases\",\"Create seller profile\",\"Priority customer support\"]", true, 2 },
                    { 2, "Pro", "Everything in Basic plus unlimited auctions and analytics", 20m, "USD", "Monthly", 25, 50, 25, "[\"Browse products and auctions\",\"Place bids and make purchases\",\"Create seller profile\",\"Priority customer support\",\"Advanced analytics dashboard\",\"Unlimited auction requests\"]", true, 3 },
                    { 3, "Enterprise", "The ultimate plan for power users and businesses", 50m, "USD", "Monthly", 100, 200, 100, "[\"Browse products and auctions\",\"Place bids and make purchases\",\"Create seller profile\",\"Priority customer support\",\"Advanced analytics dashboard\",\"Unlimited auction requests\",\"Featured listings\"]", true, 4 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionPlans");
        }
    }
}
