using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sayiad.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuctionCreatedByUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Auctions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE Auctions SET CreatedByUserId = (SELECT TOP 1 SellerId FROM Products WHERE Products.Id = Auctions.ProductId)
                WHERE CreatedByUserId = 0
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Auctions_CreatedByUserId",
                table: "Auctions",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Auctions_Users_CreatedByUserId",
                table: "Auctions",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Auctions_Users_CreatedByUserId",
                table: "Auctions");

            migrationBuilder.DropIndex(
                name: "IX_Auctions_CreatedByUserId",
                table: "Auctions");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Auctions");
        }
    }
}
