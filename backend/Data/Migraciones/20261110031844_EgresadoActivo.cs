using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaTGA.Data.Migraciones
{
    /// <inheritdoc />
    public partial class EgresadoActivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "activo",
                table: "egresado",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "activo",
                table: "egresado");
        }
    }
}
