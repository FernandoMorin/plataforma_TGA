using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace PlataformaTGA.Data.Migraciones
{
    /// <inheritdoc />
    public partial class ComiteHistorial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "comite_historial",
                columns: table => new
                {
                    id_historial = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    id_comite = table.Column<int>(type: "int", nullable: false),
                    fecha = table.Column<DateTime>(type: "date", nullable: false),
                    accion = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    rpe_sale = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true),
                    rpe_entra = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true),
                    rol_anterior = table.Column<int>(type: "int", nullable: true),
                    rol_nuevo = table.Column<int>(type: "int", nullable: true),
                    id_usuario = table.Column<int>(type: "int", nullable: true),
                    fecha_registro = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id_historial);
                    table.CheckConstraint("ck_historial_accion", "accion IN ('Alta', 'Baja', 'Reemplazo', 'Cambio de rol')");
                    table.ForeignKey(
                        name: "fk_historial_comite",
                        column: x => x.id_comite,
                        principalTable: "comite",
                        principalColumn: "id_comite",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_historial_entra",
                        column: x => x.rpe_entra,
                        principalTable: "profesor",
                        principalColumn: "rpe");
                    table.ForeignKey(
                        name: "fk_historial_rol_anterior",
                        column: x => x.rol_anterior,
                        principalTable: "CAT_rol_comite",
                        principalColumn: "id_rol_comite");
                    table.ForeignKey(
                        name: "fk_historial_rol_nuevo",
                        column: x => x.rol_nuevo,
                        principalTable: "CAT_rol_comite",
                        principalColumn: "id_rol_comite");
                    table.ForeignKey(
                        name: "fk_historial_sale",
                        column: x => x.rpe_sale,
                        principalTable: "profesor",
                        principalColumn: "rpe");
                    table.ForeignKey(
                        name: "fk_historial_usuario",
                        column: x => x.id_usuario,
                        principalTable: "usuario",
                        principalColumn: "id_usuario");
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_comite_historial_id_comite",
                table: "comite_historial",
                column: "id_comite");

            migrationBuilder.CreateIndex(
                name: "IX_comite_historial_id_usuario",
                table: "comite_historial",
                column: "id_usuario");

            migrationBuilder.CreateIndex(
                name: "IX_comite_historial_rol_anterior",
                table: "comite_historial",
                column: "rol_anterior");

            migrationBuilder.CreateIndex(
                name: "IX_comite_historial_rol_nuevo",
                table: "comite_historial",
                column: "rol_nuevo");

            migrationBuilder.CreateIndex(
                name: "IX_comite_historial_rpe_entra",
                table: "comite_historial",
                column: "rpe_entra");

            migrationBuilder.CreateIndex(
                name: "IX_comite_historial_rpe_sale",
                table: "comite_historial",
                column: "rpe_sale");

            // Los comités que ya existían: sus altas y bajas registradas pasan al historial (aproximado: sin emparejar reemplazos).
            migrationBuilder.Sql(@"INSERT INTO comite_historial (id_comite, fecha, accion, rpe_entra, rol_nuevo, fecha_registro)
                SELECT id_comite, COALESCE(fecha_asignacion, CURDATE()), 'Alta', rpe, id_rol_comite, NOW() FROM comite_profesor ORDER BY id_comite_profesor;");
            migrationBuilder.Sql(@"INSERT INTO comite_historial (id_comite, fecha, accion, rpe_sale, rol_anterior, fecha_registro)
                SELECT id_comite, fecha_baja, 'Baja', rpe, id_rol_comite, NOW() FROM comite_profesor WHERE fecha_baja IS NOT NULL ORDER BY id_comite_profesor;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "comite_historial");
        }
    }
}
