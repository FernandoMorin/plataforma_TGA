using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace PlataformaTGA.Data.Migraciones
{
    /// <inheritdoc />
    public partial class LineaInvestigacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "linea_investigacion",
                table: "tesis");

            migrationBuilder.AddColumn<int>(
                name: "id_linea_investigacion",
                table: "tesis",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CAT_linea_investigacion",
                columns: table => new
                {
                    id_linea_investigacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    nombre = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    activo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_linea_investigacion);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.InsertData(
                table: "CAT_linea_investigacion",
                columns: new[] { "id_linea_investigacion", "activo", "nombre" },
                values: new object[] { 1, true, "Tecnología y gestión del agua" });

            migrationBuilder.CreateIndex(
                name: "IX_tesis_id_linea_investigacion",
                table: "tesis",
                column: "id_linea_investigacion");

            migrationBuilder.AddForeignKey(
                name: "fk_tesis_linea",
                table: "tesis",
                column: "id_linea_investigacion",
                principalTable: "CAT_linea_investigacion",
                principalColumn: "id_linea_investigacion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_tesis_linea",
                table: "tesis");

            migrationBuilder.DropTable(
                name: "CAT_linea_investigacion");

            migrationBuilder.DropIndex(
                name: "IX_tesis_id_linea_investigacion",
                table: "tesis");

            migrationBuilder.DropColumn(
                name: "id_linea_investigacion",
                table: "tesis");

            migrationBuilder.AddColumn<string>(
                name: "linea_investigacion",
                table: "tesis",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true);
        }
    }
}
