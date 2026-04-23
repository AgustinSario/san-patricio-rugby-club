using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanPatricioRugby.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddGrupoFamiliar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsTitularGrupoFamiliar",
                table: "Socios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "GrupoFamiliarId",
                table: "Socios",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GruposFamiliares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GruposFamiliares", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Socios_GrupoFamiliarId",
                table: "Socios",
                column: "GrupoFamiliarId");

            migrationBuilder.AddForeignKey(
                name: "FK_Socios_GruposFamiliares_GrupoFamiliarId",
                table: "Socios",
                column: "GrupoFamiliarId",
                principalTable: "GruposFamiliares",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Socios_GruposFamiliares_GrupoFamiliarId",
                table: "Socios");

            migrationBuilder.DropTable(
                name: "GruposFamiliares");

            migrationBuilder.DropIndex(
                name: "IX_Socios_GrupoFamiliarId",
                table: "Socios");

            migrationBuilder.DropColumn(
                name: "EsTitularGrupoFamiliar",
                table: "Socios");

            migrationBuilder.DropColumn(
                name: "GrupoFamiliarId",
                table: "Socios");
        }
    }
}
