using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanPatricioRugby.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddCarnetPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CarnetPath",
                table: "Socios",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CarnetPath",
                table: "Socios");
        }
    }
}
