using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlataformaCreditos.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    IngresosMensuales = table.Column<double>(type: "REAL", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Solicitudes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    NombreCliente = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    MontoSolicitado = table.Column<double>(type: "REAL", nullable: false),
                    PlazoMeses = table.Column<int>(type: "INTEGER", nullable: false),
                    Finalidad = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Solicitudes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_UserId",
                table: "Clientes",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_Estado",
                table: "Solicitudes",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_UserId",
                table: "Solicitudes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropTable(
                name: "Solicitudes");
        }
    }
}
