using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sayiad.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuctionRequestSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuctionRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FishermanId = table.Column<int>(type: "int", nullable: false),
                    ReviewedByAuctioneerId = table.Column<int>(type: "int", nullable: true),
                    ResultingAuctionId = table.Column<int>(type: "int", nullable: true),
                    ProductTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProductDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ProductImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EstimatedValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    QuantityKg = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    FishType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CatchLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CatchDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuctionRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuctionRequests_Auctions_ResultingAuctionId",
                        column: x => x.ResultingAuctionId,
                        principalTable: "Auctions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuctionRequests_Users_FishermanId",
                        column: x => x.FishermanId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuctionRequests_Users_ReviewedByAuctioneerId",
                        column: x => x.ReviewedByAuctioneerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuctionRequests_FishermanId",
                table: "AuctionRequests",
                column: "FishermanId");

            migrationBuilder.CreateIndex(
                name: "IX_AuctionRequests_ResultingAuctionId",
                table: "AuctionRequests",
                column: "ResultingAuctionId");

            migrationBuilder.CreateIndex(
                name: "IX_AuctionRequests_ReviewedByAuctioneerId",
                table: "AuctionRequests",
                column: "ReviewedByAuctioneerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuctionRequests");
        }
    }
}
