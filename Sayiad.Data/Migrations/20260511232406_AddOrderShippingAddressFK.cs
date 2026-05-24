using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sayiad.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderShippingAddressFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ShippingAddressId",
                table: "CustomerOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE CustomerOrders SET ShippingAddressId = (SELECT TOP 1 Id FROM ShippingAddresses)
                WHERE ShippingAddressId = 0 AND EXISTS (SELECT 1 FROM ShippingAddresses)
            ");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerOrders_ShippingAddressId",
                table: "CustomerOrders",
                column: "ShippingAddressId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerOrders_ShippingAddresses_ShippingAddressId",
                table: "CustomerOrders",
                column: "ShippingAddressId",
                principalTable: "ShippingAddresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerOrders_ShippingAddresses_ShippingAddressId",
                table: "CustomerOrders");

            migrationBuilder.DropIndex(
                name: "IX_CustomerOrders_ShippingAddressId",
                table: "CustomerOrders");

            migrationBuilder.DropColumn(
                name: "ShippingAddressId",
                table: "CustomerOrders");
        }
    }
}
