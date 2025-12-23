using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGR.Api.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddFichaTecnicaReceitaPrincipal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ReceitaPrincipalId",
                table: "FichaTecnica",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FichaTecnica_ReceitaPrincipalId",
                table: "FichaTecnica",
                column: "ReceitaPrincipalId");

            migrationBuilder.AddForeignKey(
                name: "FK_FichaTecnica_Receita_ReceitaPrincipalId",
                table: "FichaTecnica",
                column: "ReceitaPrincipalId",
                principalTable: "Receita",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FichaTecnica_Receita_ReceitaPrincipalId",
                table: "FichaTecnica");

            migrationBuilder.DropIndex(
                name: "IX_FichaTecnica_ReceitaPrincipalId",
                table: "FichaTecnica");

            migrationBuilder.DropColumn(
                name: "ReceitaPrincipalId",
                table: "FichaTecnica");
        }
    }
}
