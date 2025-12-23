# Testes Funcionais - Validação da Implementação

## Pré-requisitos
- Banco de dados com tenant configurado
- Insumos criados com os seguintes dados (para teste "FAROFA DA VOVÓ"):
  - farinha: 500g a R$ 14/kg
  - sal: 2g a R$ 3,89/kg
  - calabresa: 150g a R$ 22,14/kg
  - salsa: 10g a R$ 40/kg
  - cenoura: 20g a R$ 7,69/kg
  - pimentão: 20g a R$ 19,69/kg
  - manteiga: 50g a R$ 42,32/kg
  - cebola: 25g a R$ 6,65/kg
- Unidade de medida GR (gramas) cadastrada
- Categoria de receita cadastrada

## Teste 1.1: Ficha nova com porção (caso planilha "FAROFA DA VOVÓ")

### Dados de entrada:
```json
{
  "nome": "FAROFA DA VOVÓ",
  "categoriaId": <ID_CATEGORIA>,
  "porcaoVendaQuantidade": 100,
  "porcaoVendaUnidadeMedidaId": <ID_UNIDADE_GR>,
  "indiceContabil": 3,
  "itens": [
    { "tipoItem": "Insumo", "insumoId": <ID_FARINHA>, "quantidade": 500, "unidadeMedidaId": <ID_GR>, "ordem": 1 },
    { "tipoItem": "Insumo", "insumoId": <ID_SAL>, "quantidade": 2, "unidadeMedidaId": <ID_GR>, "ordem": 2 },
    { "tipoItem": "Insumo", "insumoId": <ID_CALABRESA>, "quantidade": 150, "unidadeMedidaId": <ID_GR>, "ordem": 3 },
    { "tipoItem": "Insumo", "insumoId": <ID_SALSA>, "quantidade": 10, "unidadeMedidaId": <ID_GR>, "ordem": 4 },
    { "tipoItem": "Insumo", "insumoId": <ID_CENOURA>, "quantidade": 20, "unidadeMedidaId": <ID_GR>, "ordem": 5 },
    { "tipoItem": "Insumo", "insumoId": <ID_PIMENTAO>, "quantidade": 20, "unidadeMedidaId": <ID_GR>, "ordem": 6 },
    { "tipoItem": "Insumo", "insumoId": <ID_MANTEIGA>, "quantidade": 50, "unidadeMedidaId": <ID_GR>, "ordem": 7 },
    { "tipoItem": "Insumo", "insumoId": <ID_CEBOLA>, "quantidade": 25, "unidadeMedidaId": <ID_GR>, "ordem": 8 }
  ]
}
```

### Validações esperadas (GetById):
- `custoTotal` ≈ 13,55863 (tolerância ±0,0001)
- `pesoTotalBase` = 777 (exato)
- `rendimentoFinal` = 777 (sem IC/IPC)
- `custoPorUnidade` ≈ 0,01744997 (tolerância ±0,0000001)
- `custoPorPorcaoVenda` ≈ 1,744997 (tolerância ±0,000001)
- `precoMesaSugerido` ≈ 5,234992 (tolerância ±0,000001)
- Canal "Plano 12%": `precoVenda` ≈ 5,957421 (tolerância ±0,000001)
- Canal "Plano 23%": `precoVenda` ≈ 6,80549 (tolerância ±0,00001)

## Teste 1.2: Ficha antiga (sem porção definida)

### Passos:
1. Buscar uma ficha técnica existente que tenha `PorcaoVendaQuantidade = null` e `IndiceContabil` preenchido
2. Validar no GetById:
   - `PrecoSugeridoVenda` está preenchido (não null) - comportamento legado
   - `Canais` têm `PrecoVenda > 0` quando `PrecoSugeridoVenda > 0` (modo automático funcionando)

## Teste 1.3: Receita - regressão do bug FatorCorrecao²

### Passos:
1. Buscar uma receita que tenha insumo com `FatorCorrecao > 1` (ex.: 1.1)
2. Validar no GetById:
   - `QuantidadeBruta` no DTO do item = `Quantidade * FatorCorrecao` (apenas visual)
   - `CustoItem` agora usa `Quantidade` (não quantidadeBruta) no cálculo
   - `CustoTotal` e `CustoPorPorcao` refletem a correção (sem FatorCorrecao²)

## Pontos Críticos Verificados no Código

### 2.1 Margem no modo "multiplicador"
✅ **Verificado**: 
- TaxaPercentual dos canais padrão: 12 e 23 (linhas 197, 209)
- feePercent calculado corretamente: `(taxaPercentual + comissaoPercentual) / 100m` (linha 288)
- Divisão por 100m (decimal) garante precisão

### 2.2 CustoPorPorcao no cálculo de margem
✅ **Verificado**:
- Se porção definida: `custoPorPorcao = CustoPorUnidade * PorcaoVendaQuantidade` (linha 233)
- Se não há porção: `custoPorPorcao = CustoPorUnidade` (linha 238)
- Variável sempre inicializada antes do uso (linha 230)

## Como Executar os Testes

### Opção 1: Via API (Postman/Insomnia)
1. Criar ficha técnica via POST `/api/tenant/fichas-tecnicas`
2. Buscar via GET `/api/tenant/fichas-tecnicas/{id}`
3. Validar os valores calculados

### Opção 2: Via Frontend
1. Acessar o formulário de ficha técnica
2. Preencher os dados do teste
3. Salvar e verificar os valores na tela de detalhes

### Opção 3: Script de Teste (se disponível)
Executar script de teste automatizado (se criado)

