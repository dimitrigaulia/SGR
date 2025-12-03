# Plano Refinado - Refatoração Modelagem SGR

## Regras Críticas de Implementação

### 1. ExibirComoQB - Regra Visual

**REGRA FUNDAMENTAL**: `ExibirComoQB` é APENAS visual e NÃO deve influenciar cálculos.

#### Implementação:
- ✅ **Cálculos de Custo**: Sempre usar `Quantidade` numérica, ignorando completamente `ExibirComoQB`
- ✅ **Uso Permitido**: Apenas em:
  - DTOs (para transporte de dados)
  - UI/Frontend (exibir "QB" quando `true`)
  - PDF/Impressão (exibir "QB" quando `true`)
- ❌ **Uso Proibido**: Em qualquer cálculo de custo, rendimento ou preço

#### Exemplos de Código Correto:
```csharp
// ✅ CORRETO - Usa Quantidade numérica
custoLinha = item.Quantidade * custoPorUnidadeUso;

// ❌ ERRADO - NÃO fazer isso
if (!item.ExibirComoQB) {
    custoLinha = item.Quantidade * custoPorUnidadeUso;
}
```

#### Onde Implementar:
- `ReceitaService.CalcularCustos()` - usar sempre `item.Quantidade`
- `FichaTecnicaService.CalcularCustosFichaTecnica()` - usar sempre `item.Quantidade`
- `FichaTecnicaService.CalcularRendimentoFinal()` - usar sempre `item.Quantidade`
- Controllers de PDF/Print - exibir "QB" quando `ExibirComoQB = true`

---

### 2. FichaTecnica.PrecoSugeridoVenda

**REGRA**: A propriedade `PrecoSugeridoVenda` deve existir na entidade e ser calculada como:

```
PrecoSugeridoVenda = CustoPorUnidade * IndiceContabil
```

#### Implementação:
- ✅ Propriedade existe na entidade `FichaTecnica` (tipo: `decimal?`)
- ✅ Método `CalcularPrecoSugerido()` implementa a fórmula correta
- ✅ Chamado após calcular `CustoPorUnidade` e antes de calcular preços dos canais

#### Código de Referência:
```csharp
private void CalcularPrecoSugerido(FichaTecnica ficha)
{
    if (ficha.IndiceContabil.HasValue && ficha.IndiceContabil.Value > 0 && ficha.CustoPorUnidade > 0)
    {
        ficha.PrecoSugeridoVenda = Math.Round(ficha.CustoPorUnidade * ficha.IndiceContabil.Value, 4);
    }
    else
    {
        ficha.PrecoSugeridoVenda = null;
    }
}
```

#### Ordem de Execução:
1. Calcular `CustoTotal` (soma dos custos dos itens)
2. Calcular `CustoPorUnidade` (CustoTotal / RendimentoFinal)
3. Calcular `RendimentoFinal` (aplicar IC e IPC)
4. **Calcular `PrecoSugeridoVenda`** (CustoPorUnidade * IndiceContabil)
5. Calcular preços dos canais (baseado em PrecoSugeridoVenda)

---

### 3. Cálculo de RendimentoFinal

**REGRA**: Para a primeira versão, considerar APENAS itens cuja `UnidadeMedida.Sigla = "GR"`.

#### Implementação:
- ✅ Filtrar itens: `unidadeMedida.Sigla.ToUpper() == "GR"`
- ✅ Ignorar completamente itens com `Sigla = "UN"` ou `Sigla = "ML"`
- ✅ `RendimentoFinal` = peso comestível total em gramas (após aplicar IC e IPC)

#### Código de Referência:
```csharp
private void CalcularRendimentoFinal(FichaTecnica ficha, List<UnidadeMedida> unidadesMedida)
{
    var quantidadeTotalBase = 0m;

    foreach (var item in ficha.Itens)
    {
        var unidadeMedida = unidadesMedida.FirstOrDefault(u => u.Id == item.UnidadeMedidaId);
        if (unidadeMedida != null && unidadeMedida.Sigla.ToUpper() == "GR")
        {
            // IMPORTANTE: ExibirComoQB é apenas visual, sempre usar Quantidade numérica
            quantidadeTotalBase += item.Quantidade;
        }
        // Itens com UN ou ML são ignorados nesta versão
    }

    // Aplicar IC (Índice de Cocção)
    decimal pesoAposCoccao = quantidadeTotalBase;
    if (ficha.ICOperador.HasValue && ficha.ICValor.HasValue)
    {
        var icValor = Math.Clamp(ficha.ICValor.Value, 0, 9999);
        var icPercentual = icValor / 100m;

        if (ficha.ICOperador == '+')
        {
            pesoAposCoccao = quantidadeTotalBase * (1 + icPercentual);
        }
        else if (ficha.ICOperador == '-')
        {
            pesoAposCoccao = quantidadeTotalBase * (1 - icPercentual);
        }
    }

    // Aplicar IPC (Índice de Partes Comestíveis)
    decimal pesoComestivel = pesoAposCoccao;
    if (ficha.IPCValor.HasValue)
    {
        var ipcValor = Math.Clamp(ficha.IPCValor.Value, 0, 999);
        var ipcPercentual = ipcValor / 100m;
        pesoComestivel = pesoAposCoccao * ipcPercentual;
    }

    ficha.RendimentoFinal = pesoComestivel;
}
```

#### Nota para Versões Futuras:
- Em versões futuras, pode-se considerar conversões entre unidades (ex: ML para GR)
- Por enquanto, apenas GR é considerado para simplificar

---

## Checklist de Validação

### ExibirComoQB
- [x] Não usado em cálculos de custo
- [x] Não usado em cálculos de rendimento
- [x] Usado apenas em DTOs
- [x] Usado apenas em exibição (UI/PDF)

### PrecoSugeridoVenda
- [x] Propriedade existe na entidade
- [x] Calculado como: CustoPorUnidade * IndiceContabil
- [x] Chamado na ordem correta (após CustoPorUnidade, antes de canais)

### RendimentoFinal
- [x] Considera apenas itens com Sigla = "GR"
- [x] Ignora itens com UN/ML
- [x] Aplica IC corretamente
- [x] Aplica IPC corretamente
- [x] Resultado em gramas

---

## Estrutura de Cálculos - Ordem de Execução

```
1. CalcularRendimentoFinal()
   └─> Soma quantidades (apenas GR)
   └─> Aplica IC (se definido)
   └─> Aplica IPC (se definido)
   └─> Atribui RendimentoFinal

2. CalcularCustosFichaTecnica()
   └─> Para cada item:
       ├─> Se Insumo: Quantidade * CustoPorUnidadeUso
       └─> Se Receita: Quantidade * CustoPorPorcao
   └─> Soma = CustoTotal
   └─> CustoPorUnidade = CustoTotal / RendimentoFinal
       ⚠️ PROTEÇÃO: Se RendimentoFinal for null ou <= 0:
          └─> CustoPorUnidade = 0 (não calcular divisão)

3. CalcularPrecoSugerido()
   └─> Verifica se RendimentoFinal > 0
   └─> Se não: PrecoSugeridoVenda = null (não calcula)
   └─> Se sim: PrecoSugeridoVenda = CustoPorUnidade * IndiceContabil

4. CalcularPrecosCanais()
   └─> Para cada canal:
       └─> PrecoVenda = PrecoSugeridoVenda * (1 + TaxaPercentual/100)
       └─> Calcula MargemCalculadaPercentual
```

### Proteções Contra Divisão por Zero

**CRÍTICO**: Sempre proteger divisões por zero/nulo para evitar exceções.

#### CalcularCustosFichaTecnica:
```csharp
// ✅ CORRETO - Proteção implementada
if (ficha.RendimentoFinal.HasValue && ficha.RendimentoFinal.Value > 0)
{
    ficha.CustoPorUnidade = custoTotal / ficha.RendimentoFinal.Value;
}
else
{
    ficha.CustoPorUnidade = 0; // Não calcular se RendimentoFinal inválido
}
```

#### CalcularPrecoSugerido:
```csharp
// ✅ CORRETO - Não calcula se RendimentoFinal inválido
if (!ficha.RendimentoFinal.HasValue || ficha.RendimentoFinal.Value <= 0)
{
    ficha.PrecoSugeridoVenda = null;
    return;
}
```

---

## Observações Importantes

1. **ExibirComoQB**: Esta flag é puramente cosmética. Nunca deve afetar cálculos numéricos.

2. **RendimentoFinal**: Por enquanto, apenas gramas são considerados. Itens em outras unidades (UN, ML) são ignorados no cálculo, mas ainda aparecem na composição da ficha técnica.

3. **PrecoSugeridoVenda**: É o preço base antes de aplicar taxas dos canais. Os canais aplicam suas próprias taxas sobre este valor.

4. **Ordem de Cálculo**: A ordem é crítica. RendimentoFinal deve ser calculado primeiro, pois é usado para calcular CustoPorUnidade.

5. **Proteção Divisão por Zero**: 
   - Sempre verificar se RendimentoFinal é null ou <= 0 antes de dividir
   - Se inválido, definir CustoPorUnidade = 0 e PrecoSugeridoVenda = null
   - Nunca deixar exceções de divisão por zero ocorrerem

6. **ICOperador - Coerência de Tipo**:
   - Tipo na entidade: `char? ICOperador`
   - Tipo nos DTOs: `char? ICOperador`
   - Tipo no SQL: `CHAR(1)`
   - Comparação no código: `ficha.ICOperador == '+'` (char, não string)
   - ✅ Tudo coerente e consistente

