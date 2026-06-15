using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sayiad.Data.Migrations
{
    /// <inheritdoc />
    public partial class AlignFinancialSystemToPlanSpec : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_Users_UserId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_UserId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Reports");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "Payments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Payments",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<DateTime>(
                name: "FreezeUntil",
                table: "Payments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_FreezeUntil",
                table: "Payments",
                column: "FreezeUntil",
                filter: "[FreezeUntil] IS NOT NULL");

            // Update SystemTransactionType enum string values to match plan spec
            migrationBuilder.Sql(@"
UPDATE [SystemWalletTransactions] SET [Type] = 'PlatformFeeCredit' WHERE [Type] = 'ProductFeeReceived';
UPDATE [SystemWalletTransactions] SET [Type] = 'PlatformFeeRefunded' WHERE [Type] = 'ProductFeeRefunded';
UPDATE [SystemWalletTransactions] SET [Type] = 'AuctioneerFeeCredit' WHERE [Type] = 'AuctionFeeReceived';
UPDATE [SystemWalletTransactions] SET [Type] = 'SubscriptionRevenueCredit' WHERE [Type] = 'SubscriptionReceived';
UPDATE [SystemWalletTransactions] SET [Type] = 'AuctioneerFeeWithdrawal' WHERE [Type] = 'AdminWithdrawal';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_FreezeUntil",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "FreezeUntil",
                table: "Payments");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Reports",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PaymentMethod",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Payments",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_UserId",
                table: "Reports",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_Users_UserId",
                table: "Reports",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");

            // Revert SystemTransactionType enum string values to original
            migrationBuilder.Sql(@"
UPDATE [SystemWalletTransactions] SET [Type] = 'ProductFeeReceived' WHERE [Type] = 'PlatformFeeCredit';
UPDATE [SystemWalletTransactions] SET [Type] = 'ProductFeeRefunded' WHERE [Type] = 'PlatformFeeRefunded';
UPDATE [SystemWalletTransactions] SET [Type] = 'AuctionFeeReceived' WHERE [Type] = 'AuctioneerFeeCredit';
UPDATE [SystemWalletTransactions] SET [Type] = 'SubscriptionReceived' WHERE [Type] = 'SubscriptionRevenueCredit';
UPDATE [SystemWalletTransactions] SET [Type] = 'AdminWithdrawal' WHERE [Type] = 'AuctioneerFeeWithdrawal';
");
        }
    }
}
