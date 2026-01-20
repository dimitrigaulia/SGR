using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGR.Api.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddInsumoUnidadesPesoPorUnidadeAndRendimentoPorcoesNumero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "UnidadesPorEmbalagem",
                table: "Insumo",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PesoPorUnidade",
                table: "Insumo",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RendimentoPorcoesNumero",
                table: "FichaTecnica",
                type: "numeric",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""FichaTecnica""
                SET ""RendimentoPorcoesNumero"" = NULLIF(REPLACE(substring(""RendimentoPorcoes"" from '([0-9]+([\\.,][0-9]+)?)'), ',', '.'), '')::numeric
                WHERE ""RendimentoPorcoesNumero"" IS NULL
                  AND ""RendimentoPorcoes"" IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnidadesPorEmbalagem",
                table: "Insumo");

            migrationBuilder.DropColumn(
                name: "PesoPorUnidade",
                table: "Insumo");

            migrationBuilder.DropColumn(
                name: "RendimentoPorcoesNumero",
                table: "FichaTecnica");
        }
    }
}
