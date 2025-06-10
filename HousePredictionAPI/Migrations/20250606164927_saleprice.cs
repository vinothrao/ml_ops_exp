using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HousePredictionAPI.Migrations
{
    /// <inheritdoc />
    public partial class saleprice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SalePrice",
                table: "HouseDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SalePrice",
                table: "HouseDetails");
        }
    }
}
