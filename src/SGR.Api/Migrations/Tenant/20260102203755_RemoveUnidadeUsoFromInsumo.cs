using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGR.Api.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class RemoveUnidadeUsoFromInsumo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Insumo_UnidadeMedida_UnidadeUsoId",
                table: "Insumo");

            migrationBuilder.DropIndex(
                name: "IX_Insumo_UnidadeUsoId",
                table: "Insumo");

            migrationBuilder.DropColumn(
                name: "UnidadeUsoId",
                table: "Insumo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "UnidadeUsoId",
                table: "Insumo",
                type: "bigint",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_Insumo_UnidadeUsoId",
                table: "Insumo",
                column: "UnidadeUsoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Insumo_UnidadeMedida_UnidadeUsoId",
                table: "Insumo",
                column: "UnidadeUsoId",
                principalTable: "UnidadeMedida",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
