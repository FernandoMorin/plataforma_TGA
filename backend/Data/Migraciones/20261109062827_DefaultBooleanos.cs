using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaTGA.Data.Migraciones
{
    /// <inheritdoc />
    public partial class DefaultBooleanos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "externo",
                table: "profesor_detalle",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<bool>(
                name: "verificado",
                table: "documento",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false,
                comment: "Constancia revisada por el coordinador",
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldComment: "Constancia revisada por el coordinador");

            migrationBuilder.AlterColumn<bool>(
                name: "pendiente_conciliar",
                table: "alumno",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false,
                comment: "Alumno temporal: aún no está en el registro de la universidad",
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldComment: "Alumno temporal: aún no está en el registro de la universidad");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "externo",
                table: "profesor_detalle",
                type: "tinyint(1)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "verificado",
                table: "documento",
                type: "tinyint(1)",
                nullable: false,
                comment: "Constancia revisada por el coordinador",
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldDefaultValue: false,
                oldComment: "Constancia revisada por el coordinador");

            migrationBuilder.AlterColumn<bool>(
                name: "pendiente_conciliar",
                table: "alumno",
                type: "tinyint(1)",
                nullable: false,
                comment: "Alumno temporal: aún no está en el registro de la universidad",
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldDefaultValue: false,
                oldComment: "Alumno temporal: aún no está en el registro de la universidad");
        }
    }
}
