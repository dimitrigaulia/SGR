using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SGR.Api.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddCanalVenda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CanalVendaId",
                table: "FichaTecnicaCanal",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CanalVenda",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "text", nullable: false),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: true),
                    TaxaPercentualPadrao = table.Column<decimal>(type: "numeric", nullable: true),
                    ComissaoPercentualPadrao = table.Column<decimal>(type: "numeric", nullable: true),
                    MultiplicadorPadrao = table.Column<decimal>(type: "numeric", nullable: true),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    IsAtivo = table.Column<bool>(type: "boolean", nullable: false),
                    UsuarioCriacao = table.Column<string>(type: "text", nullable: true),
                    UsuarioAtualizacao = table.Column<string>(type: "text", nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataAtualizacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanalVenda", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FichaTecnicaCanal_CanalVendaId",
                table: "FichaTecnicaCanal",
                column: "CanalVendaId");

            migrationBuilder.CreateIndex(
                name: "IX_CanalVenda_Codigo",
                table: "CanalVenda",
                column: "Codigo",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FichaTecnicaCanal_CanalVenda_CanalVendaId",
                table: "FichaTecnicaCanal",
                column: "CanalVendaId",
                principalTable: "CanalVenda",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Migração de dados: criar canais a partir dos valores únicos existentes em FichaTecnicaCanal
            migrationBuilder.Sql(@"
                INSERT INTO ""CanalVenda"" (""Codigo"", ""Nome"", ""IsAtivo"", ""UsuarioCriacao"", ""DataCriacao"")
                SELECT DISTINCT 
                    COALESCE(""Canal"", 'CANAL_DESCONHECIDO') as ""Codigo"",
                    COALESCE(""Canal"", 'Canal Desconhecido') as ""Nome"",
                    true as ""IsAtivo"",
                    'Sistema' as ""UsuarioCriacao"",
                    NOW() as ""DataCriacao""
                FROM ""FichaTecnicaCanal""
                WHERE ""Canal"" IS NOT NULL AND ""Canal"" != ''
                ON CONFLICT (""Codigo"") DO NOTHING;
            ");

            // Atualizar FichaTecnicaCanal para referenciar os canais criados
            migrationBuilder.Sql(@"
                UPDATE ""FichaTecnicaCanal"" ftc
                SET ""CanalVendaId"" = cv.""Id""
                FROM ""CanalVenda"" cv
                WHERE ftc.""Canal"" = cv.""Codigo""
                AND ftc.""CanalVendaId"" IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FichaTecnicaCanal_CanalVenda_CanalVendaId",
                table: "FichaTecnicaCanal");

            migrationBuilder.DropTable(
                name: "CanalVenda");

            migrationBuilder.DropIndex(
                name: "IX_FichaTecnicaCanal_CanalVendaId",
                table: "FichaTecnicaCanal");

            migrationBuilder.DropColumn(
                name: "CanalVendaId",
                table: "FichaTecnicaCanal");
        }
    }
}
