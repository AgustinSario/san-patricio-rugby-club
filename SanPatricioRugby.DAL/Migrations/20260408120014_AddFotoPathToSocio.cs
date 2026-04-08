using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanPatricioRugby.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddFotoPathToSocio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FotoPath",
                table: "Socios",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FotoPath",
                table: "Socios");
        }
    }
}
