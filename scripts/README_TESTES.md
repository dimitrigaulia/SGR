# Scripts de Teste - Validação da Ficha Técnica

Este diretório contém scripts para validar a implementação da Ficha Técnica conforme os requisitos da planilha "FAROFA DA VOVÓ".

## Pré-requisitos

1. **Banco de dados configurado**:
   - PostgreSQL rodando
   - ConnectionStrings configuradas em `src/SGR.Api/appsettings.json`
   - Pelo menos um tenant ativo criado no sistema

2. **Dados básicos**:
   - O script criará automaticamente:
     - Unidade de medida GR (se não existir)
     - Insumos necessários para o teste
   - Você precisa ter:
     - Pelo menos uma categoria de insumo ativa
     - Pelo menos uma categoria de receita ativa

## Como Executar

### Opção 1: Script PowerShell (Recomendado)

Execute o script PowerShell que cria um projeto console temporário e executa os testes:

```powershell
.\scripts\TesteFichaTecnica.ps1
```

O script irá:
1. Criar um projeto console temporário
2. Adicionar referências ao projeto SGR.Api
3. Compilar o projeto
4. Executar os testes
5. Limpar os arquivos temporários

### Opção 2: Executar o arquivo C# diretamente (se tiver dotnet-script instalado)

```bash
dotnet script scripts/TesteFichaTecnica.cs
```

**Nota**: Requer instalação do `dotnet-script`:
```bash
dotnet tool install -g dotnet-script
```

## O que o teste valida

O teste cria uma ficha técnica "FAROFA DA VOVÓ" com os seguintes ingredientes:

| Ingrediente | Quantidade (GR) | Custo (R$/kg) |
|-------------|----------------|---------------|
| Farinha     | 500g           | 14,00         |
| Sal         | 2g             | 3,89          |
| Calabresa   | 150g           | 22,14         |
| Salsa       | 10g            | 40,00         |
| Cenoura     | 20g            | 7,69          |
| Pimentão    | 20g            | 19,69         |
| Manteiga    | 50g            | 42,32         |
| Cebola      | 25g            | 6,65          |

**Parâmetros da ficha**:
- Porção de venda: 100g
- Markup (IndiceContabil): 3x

## Validações Esperadas

O teste valida os seguintes cálculos:

1. **CustoTotal**: ≈ 13,55863
2. **PesoTotalBase**: = 777g (antes de IC/IPC)
3. **RendimentoFinal**: = 777g (sem IC/IPC aplicado)
4. **CustoPorUnidade**: ≈ 0,01744997 (custo por grama)
5. **CustoPorPorcaoVenda**: ≈ 1,744997 (custo por 100g)
6. **PrecoMesaSugerido**: ≈ 5,234992 (custo por porção × markup)
7. **Canal Plano 12%**: ≈ 5,957421 (preço mesa × 1.138)
8. **Canal Plano 23%**: ≈ 6,80549 (preço mesa × 1.3)

## Tolerâncias

- Valores monetários: ±0,0001
- Valores de peso: ±1g

## Saída do Teste

O teste exibe:
- Status de cada validação (✓ OK ou ✗ FALHOU)
- Valores calculados vs. esperados
- Resumo final com total de validações aprovadas/reprovadas

## Troubleshooting

### Erro: "Nenhum tenant ativo encontrado"
- Crie um tenant através do backoffice ou diretamente no banco de dados

### Erro: "Nenhuma categoria de insumo/receita encontrada"
- Crie pelo menos uma categoria de insumo e uma categoria de receita através da interface ou banco de dados

### Erro de conexão com banco de dados
- Verifique as ConnectionStrings em `src/SGR.Api/appsettings.json`
- Certifique-se de que o PostgreSQL está rodando
- Verifique usuário e senha do banco

### Erro de compilação
- Certifique-se de que o projeto `SGR.Api` compila sem erros
- Execute `dotnet build` no diretório `src/SGR.Api` primeiro

## Próximos Testes

Após validar o teste principal, você pode executar manualmente:

1. **Teste de ficha antiga (sem porção)**:
   - Criar uma ficha sem `PorcaoVendaQuantidade`
   - Validar que `PrecoSugeridoVenda` continua calculando (comportamento legado)

2. **Teste de receita com FatorCorrecao**:
   - Criar uma receita com insumo que tenha `FatorCorrecao > 1`
   - Validar que `QuantidadeBruta` = `Quantidade × FatorCorrecao` (apenas visual)
   - Validar que `CustoItem` não aplica FatorCorrecao duas vezes



