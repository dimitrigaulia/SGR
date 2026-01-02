using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SGR.Api.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class SimplifyCanalVenda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remover índice único de Codigo antes de remover a coluna
            migrationBuilder.DropIndex(
                name: "IX_CanalVenda_Codigo",
                table: "CanalVenda");

            // Remover colunas desnecessárias
            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "CanalVenda");

            migrationBuilder.DropColumn(
                name: "ComissaoPercentualPadrao",
                table: "CanalVenda");

            migrationBuilder.DropColumn(
                name: "Descricao",
                table: "CanalVenda");

            migrationBuilder.DropColumn(
                name: "MultiplicadorPadrao",
                table: "CanalVenda");

            migrationBuilder.DropColumn(
                name: "Observacoes",
                table: "CanalVenda");

            // Criar índice único de Nome
            migrationBuilder.CreateIndex(
                name: "IX_CanalVenda_Nome",
                table: "CanalVenda",
                column: "Nome",
                unique: true);

            // Criar canais padrão se não existirem
            migrationBuilder.Sql(@"
                INSERT INTO ""CanalVenda"" (""Nome"", ""TaxaPercentualPadrao"", ""IsAtivo"", ""UsuarioCriacao"", ""DataCriacao"")
                VALUES 
                    ('iFood 1', 13, true, 'Sistema', NOW() AT TIME ZONE 'utc'),
                    ('iFood 2', 25, true, 'Sistema', NOW() AT TIME ZONE 'utc'),
                    ('Balcão', 0, true, 'Sistema', NOW() AT TIME ZONE 'utc'),
                    ('Delivery Próprio', 0, true, 'Sistema', NOW() AT TIME ZONE 'utc')
                ON CONFLICT (""Nome"") DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CanalVenda_Nome",
                table: "CanalVenda");

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "CanalVenda",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ComissaoPercentualPadrao",
                table: "CanalVenda",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Descricao",
                table: "CanalVenda",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MultiplicadorPadrao",
                table: "CanalVenda",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Observacoes",
                table: "CanalVenda",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CanalVenda_Codigo",
                table: "CanalVenda",
                column: "Codigo",
                unique: true);
        }
    }
}
