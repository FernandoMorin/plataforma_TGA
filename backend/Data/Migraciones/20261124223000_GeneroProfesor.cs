using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaTGA.Data.Migraciones
{
    /// <inheritdoc />
    public partial class GeneroProfesor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "genero",
                table: "profesor",
                type: "varchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Masculino");

            migrationBuilder.AddCheckConstraint(
                name: "ck_profesor_genero",
                table: "profesor",
                sql: "genero IN ('Masculino', 'Femenino')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_profesor_genero",
                table: "profesor");

            migrationBuilder.DropColumn(
                name: "genero",
                table: "profesor");
        }
    }
}
