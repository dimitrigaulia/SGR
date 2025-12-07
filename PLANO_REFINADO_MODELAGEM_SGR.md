# Plano Refinado - RefatoraÃ§Ã£o Modelagem SGR

## Regras CrÃ­ticas de ImplementaÃ§Ã£o

### 1. ExibirComoQB - Regra Visual

**REGRA FUNDAMENTAL**: `ExibirComoQB` Ã© APENAS visual e NÃƒO deve influenciar cÃ¡lculos.

#### ImplementaÃ§Ã£o:
- âœ… **CÃ¡lculos de Custo**: Sempre usar `Quantidade` numÃ©rica, ignorando completamente `ExibirComoQB`
- âœ… **Uso Permitido**: Apenas em:
  - DTOs (para transporte de dados)
  - UI/Frontend (exibir "QB" quando `true`)
  - PDF/ImpressÃ£o (exibir "QB" quando `true`)
- âŒ **Uso Proibido**: Em qualquer cÃ¡lculo de custo, rendimento ou preÃ§o

#### Exemplos de CÃ³digo Correto:
```csharp
// âœ… CORRETO - Usa Quantidade numÃ©rica
custoLinha = item.Quantidade * custoPorUnidadeUso;

// âŒ ERRADO - NÃƒO fazer isso
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

#### ImplementaÃ§Ã£o:
- âœ… Propriedade existe na entidade `FichaTecnica` (tipo: `decimal?`)
- âœ… MÃ©todo `CalcularPrecoSugerido()` implementa a fÃ³rmula correta
- âœ… Chamado apÃ³s calcular `CustoPorUnidade` e antes de calcular preÃ§os dos canais

#### CÃ³digo de ReferÃªncia:
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

#### Ordem de ExecuÃ§Ã£o:
1. Calcular `CustoTotal` (soma dos custos dos itens)
2. Calcular `CustoPorUnidade` (CustoTotal / RendimentoFinal)
3. Calcular `RendimentoFinal` (aplicar IC e IPC)
4. **Calcular `PrecoSugeridoVenda`** (CustoPorUnidade * IndiceContabil)
5. Calcular preÃ§os dos canais (baseado em PrecoSugeridoVenda)

---

### 3. CÃ¡lculo de RendimentoFinal

**REGRA**: Para a primeira versÃ£o, considerar APENAS itens cuja `UnidadeMedida.Sigla = "GR"`.

#### ImplementaÃ§Ã£o:
- âœ… Filtrar itens: `unidadeMedida.Sigla.ToUpper() == "GR"`
- âœ… Ignorar completamente itens com `Sigla = "UN"` ou `Sigla = "ML"`
- âœ… `RendimentoFinal` = peso comestÃ­vel total em gramas (apÃ³s aplicar IC e IPC)

#### CÃ³digo de ReferÃªncia:
```csharp
private void CalcularRendimentoFinal(FichaTecnica ficha, List<UnidadeMedida> unidadesMedida)
{
    var quantidadeTotalBase = 0m;

    foreach (var item in ficha.Itens)
    {
        var unidadeMedida = unidadesMedida.FirstOrDefault(u => u.Id == item.UnidadeMedidaId);
        if (unidadeMedida != null && unidadeMedida.Sigla.ToUpper() == "GR")
        {
            // IMPORTANTE: ExibirComoQB Ã© apenas visual, sempre usar Quantidade numÃ©rica
            quantidadeTotalBase += item.Quantidade;
        }
        // Itens com UN ou ML sÃ£o ignorados nesta versÃ£o
    }

    // Aplicar IC (Ãndice de CocÃ§Ã£o)
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

    // Aplicar IPC (Ãndice de Partes ComestÃ­veis)
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

#### Nota para VersÃµes Futuras:
- Em versÃµes futuras, pode-se considerar conversÃµes entre unidades (ex: ML para GR)
- Por enquanto, apenas GR Ã© considerado para simplificar

---

## Checklist de ValidaÃ§Ã£o

### ExibirComoQB
- [x] NÃ£o usado em cÃ¡lculos de custo
- [x] NÃ£o usado em cÃ¡lculos de rendimento
- [x] Usado apenas em DTOs
- [x] Usado apenas em exibiÃ§Ã£o (UI/PDF)

### PrecoSugeridoVenda
- [x] Propriedade existe na entidade
- [x] Calculado como: CustoPorUnidade * IndiceContabil
- [x] Chamado na ordem correta (apÃ³s CustoPorUnidade, antes de canais)

### RendimentoFinal
- [x] Considera apenas itens com Sigla = "GR"
- [x] Ignora itens com UN/ML
- [x] Aplica IC corretamente
- [x] Aplica IPC corretamente
- [x] Resultado em gramas

---

## Estrutura de CÃ¡lculos - Ordem de ExecuÃ§Ã£o

```
1. CalcularRendimentoFinal()
   â””â”€> Soma quantidades (apenas GR)
   â””â”€> Aplica IC (se definido)
   â””â”€> Aplica IPC (se definido)
   â””â”€> Atribui RendimentoFinal

2. CalcularCustosFichaTecnica()
   â””â”€> Para cada item:
       â”œâ”€> Se Insumo: Quantidade * CustoPorUnidadeUso
       â””â”€> Se Receita: Quantidade * CustoPorPorcao
   â””â”€> Soma = CustoTotal
   â””â”€> CustoPorUnidade = CustoTotal / RendimentoFinal
       âš ï¸ PROTEÃ‡ÃƒO: Se RendimentoFinal for null ou <= 0:
          â””â”€> CustoPorUnidade = 0 (nÃ£o calcular divisÃ£o)

3. CalcularPrecoSugerido()
   â””â”€> Verifica se RendimentoFinal > 0
   â””â”€> Se nÃ£o: PrecoSugeridoVenda = null (nÃ£o calcula)
   â””â”€> Se sim: PrecoSugeridoVenda = CustoPorUnidade * IndiceContabil

4. CalcularPrecosCanais()
   â””â”€> Para cada canal:
       â””â”€> PrecoVenda = PrecoSugeridoVenda * (1 + TaxaPercentual/100)
       â””â”€> Calcula MargemCalculadaPercentual
```

### ProteÃ§Ãµes Contra DivisÃ£o por Zero

**CRÃTICO**: Sempre proteger divisÃµes por zero/nulo para evitar exceÃ§Ãµes.

#### CalcularCustosFichaTecnica:
```csharp
// âœ… CORRETO - ProteÃ§Ã£o implementada
if (ficha.RendimentoFinal.HasValue && ficha.RendimentoFinal.Value > 0)
{
    ficha.CustoPorUnidade = custoTotal / ficha.RendimentoFinal.Value;
}
else
{
    ficha.CustoPorUnidade = 0; // NÃ£o calcular se RendimentoFinal invÃ¡lido
}
```

#### CalcularPrecoSugerido:
```csharp
// âœ… CORRETO - NÃ£o calcula se RendimentoFinal invÃ¡lido
if (!ficha.RendimentoFinal.HasValue || ficha.RendimentoFinal.Value <= 0)
{
    ficha.PrecoSugeridoVenda = null;
    return;
}
```

---

## ObservaÃ§Ãµes Importantes

1. **ExibirComoQB**: Esta flag Ã© puramente cosmÃ©tica. Nunca deve afetar cÃ¡lculos numÃ©ricos.

2. **RendimentoFinal**: Por enquanto, apenas gramas sÃ£o considerados. Itens em outras unidades (UN, ML) sÃ£o ignorados no cÃ¡lculo, mas ainda aparecem na composiÃ§Ã£o da ficha tÃ©cnica.

3. **PrecoSugeridoVenda**: Ã‰ o preÃ§o base antes de aplicar taxas dos canais. Os canais aplicam suas prÃ³prias taxas sobre este valor.

4. **Ordem de CÃ¡lculo**: A ordem Ã© crÃ­tica. RendimentoFinal deve ser calculado primeiro, pois Ã© usado para calcular CustoPorUnidade.

5. **ProteÃ§Ã£o DivisÃ£o por Zero**: 
   - Sempre verificar se RendimentoFinal Ã© null ou <= 0 antes de dividir
   - Se invÃ¡lido, definir CustoPorUnidade = 0 e PrecoSugeridoVenda = null
   - Nunca deixar exceÃ§Ãµes de divisÃ£o por zero ocorrerem

6. **ICOperador - CoerÃªncia de Tipo**:
   - Tipo na entidade: `char? ICOperador`
   - Tipo nos DTOs: `char? ICOperador`
   - Tipo no SQL: `CHAR(1)`
   - ComparaÃ§Ã£o no cÃ³digo: `ficha.ICOperador == '+'` (char, nÃ£o string)
   - âœ… Tudo coerente e consistente

