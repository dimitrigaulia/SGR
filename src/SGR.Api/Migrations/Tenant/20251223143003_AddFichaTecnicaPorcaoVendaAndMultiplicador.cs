using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGR.Api.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddFichaTecnicaPorcaoVendaAndMultiplicador : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PorcaoVendaQuantidade",
                table: "FichaTecnica",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PorcaoVendaUnidadeMedidaId",
                table: "FichaTecnica",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RendimentoPorcoes",
                table: "FichaTecnica",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Multiplicador",
                table: "FichaTecnicaCanal",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FichaTecnica_PorcaoVendaUnidadeMedidaId",
                table: "FichaTecnica",
                column: "PorcaoVendaUnidadeMedidaId");

            migrationBuilder.AddForeignKey(
                name: "FK_FichaTecnica_UnidadeMedida_PorcaoVendaUnidadeMedidaId",
                table: "FichaTecnica",
                column: "PorcaoVendaUnidadeMedidaId",
                principalTable: "UnidadeMedida",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FichaTecnica_UnidadeMedida_PorcaoVendaUnidadeMedidaId",
                table: "FichaTecnica");

            migrationBuilder.DropIndex(
                name: "IX_FichaTecnica_PorcaoVendaUnidadeMedidaId",
                table: "FichaTecnica");

            migrationBuilder.DropColumn(
                name: "PorcaoVendaQuantidade",
                table: "FichaTecnica");

            migrationBuilder.DropColumn(
                name: "PorcaoVendaUnidadeMedidaId",
                table: "FichaTecnica");

            migrationBuilder.DropColumn(
                name: "RendimentoPorcoes",
                table: "FichaTecnica");

            migrationBuilder.DropColumn(
                name: "Multiplicador",
                table: "FichaTecnicaCanal");
        }
    }
}
