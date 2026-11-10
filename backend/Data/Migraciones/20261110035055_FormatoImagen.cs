using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaTGA.Data.Migraciones
{
    /// <inheritdoc />
    public partial class FormatoImagen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "CAT_formato_reporte",
                columns: new[] { "id_formato_reporte", "nombre" },
                values: new object[] { 3, "Imagen" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "CAT_formato_reporte",
                keyColumn: "id_formato_reporte",
                keyValue: 3);
        }
    }
}
