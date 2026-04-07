using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanPatricioRugby.DAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEmailConfigClubData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CondicionIva",
                table: "ConfiguracionesEmail",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Cuit",
                table: "ConfiguracionesEmail",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Domicilio",
                table: "ConfiguracionesEmail",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IngresosBrutos",
                table: "ConfiguracionesEmail",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InicioActividades",
                table: "ConfiguracionesEmail",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NombreClub",
                table: "ConfiguracionesEmail",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RazonSocial",
                table: "ConfiguracionesEmail",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CondicionIva",
                table: "ConfiguracionesEmail");

            migrationBuilder.DropColumn(
                name: "Cuit",
                table: "ConfiguracionesEmail");

            migrationBuilder.DropColumn(
                name: "Domicilio",
                table: "ConfiguracionesEmail");

            migrationBuilder.DropColumn(
                name: "IngresosBrutos",
                table: "ConfiguracionesEmail");

            migrationBuilder.DropColumn(
                name: "InicioActividades",
                table: "ConfiguracionesEmail");

            migrationBuilder.DropColumn(
                name: "NombreClub",
                table: "ConfiguracionesEmail");

            migrationBuilder.DropColumn(
                name: "RazonSocial",
                table: "ConfiguracionesEmail");
        }
    }
}
