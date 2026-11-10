using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaTGA.Data.Migraciones
{
    /// <inheritdoc />
    public partial class InformePorPeriodo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "uq_revision_periodo",
                table: "revision",
                columns: new[] { "id_tesis", "id_periodo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_revision_periodo",
                table: "revision");
        }
    }
}
