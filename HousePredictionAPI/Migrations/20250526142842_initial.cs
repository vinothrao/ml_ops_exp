using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HousePredictionAPI.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Todos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Neighborhood = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    YearBuilt = table.Column<int>(type: "int", nullable: false),
                    TotalBsmtSF = table.Column<int>(type: "int", nullable: false),
                    GrLivArea = table.Column<int>(type: "int", nullable: false),
                    OverallQual = table.Column<int>(type: "int", nullable: false),
                    FullBath = table.Column<int>(type: "int", nullable: false),
                    TotRmsAbvGrd = table.Column<int>(type: "int", nullable: false),
                    GarageArea = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Todos", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Todos");
        }
    }
}
