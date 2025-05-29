using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace juezprueba.Migrations
{
    /// <inheritdoc />
    public partial class DbRanking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Dificultad",
                table: "Problemas",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Puntos",
                table: "Problemas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ProblemasResueltos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProblemaId = table.Column<int>(type: "int", nullable: false),
                    FechaResolucion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProblemasResueltos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProblemasResueltos_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProblemasResueltos_Problemas_ProblemaId",
                        column: x => x.ProblemaId,
                        principalTable: "Problemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProblemasResueltos_ProblemaId",
                table: "ProblemasResueltos",
                column: "ProblemaId");

            migrationBuilder.CreateIndex(
                name: "IX_ProblemasResueltos_UsuarioId",
                table: "ProblemasResueltos",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProblemasResueltos");

            migrationBuilder.DropColumn(
                name: "Dificultad",
                table: "Problemas");

            migrationBuilder.DropColumn(
                name: "Puntos",
                table: "Problemas");
        }
    }
}
