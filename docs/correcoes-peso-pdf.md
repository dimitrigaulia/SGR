# Correções: Peso no PDF e Normalização por Unidade Base

## Data: 2026-01-26

## Problema Identificado

### Sintomas:
1. **Operação/Tela**: "Peso final estimado" e "Peso por porção" apareciam vazios (—)
2. **PDF**: Exibia "Peso por Porção: 1 g" como fallback incorreto (valor inventado quando não havia cálculo real)
3. **Inconsistência**: Sistema não calculava peso quando itens eram UN sem `PesoPorUnidade`

### Causa Raiz:
- PDF não verificava se havia peso calculado antes de exibir valores
- Faltava tratamento condicional para modo unidade vs modo porção no PDF
- Tabela de composição não mostrava breakdown de peso por item

## Regras Implementadas

### Quando TODOS os itens têm peso calculável:
- **GR/ML**: usa valor direto
- **KG/L**: converte para g/ml (× 1000)
- **UN com PesoPorUnidade**: converte para g/ml via `PesoPorUnidadeGml`
- **Receita com PesoPorPorcao**: usa `PesoPorPorcao` da receita

**Resultado:** Peso total = soma dos pesos em g/ml → Peso por porção = total ÷ rendimento

### Quando ALGUM item NÃO tem peso calculável:
- **UN sem PesoPorUnidade**: não pode calcular peso
- **Receita sem PesoPorPorcao**: não pode calcular peso

**Resultado:** Peso total e Peso por porção = **"—"** (nunca "1 g" inventado)

## Mudanças Implementadas

### 1. PDF - Seção "Porção de Venda" Condicional

#### Arquivo: `PdfService.cs` (Linhas ~439-490)

**ANTES:**
```csharp
porcao.Item().Text("🍽 Porção de Venda (1 venda = quanto?)")
// Sempre mostrava "1 Porção de venda" e "Custo por porção"
```

**DEPOIS:**
```csharp
// Detecta modo unidade vs modo porção
var isModoUnidade = ficha.RendimentoPorcoesNumero.HasValue 
                    && ficha.RendimentoPorcoesNumero.Value > 0;

var tituloSecao = isModoUnidade 
    ? "🍽 Venda por Unidade"
    : "🍽 Porção de Venda (1 venda = quanto?)";

// MODO UNIDADE:
// - "1 Unidade = Hot Dog"
// - "Custo por unidade: R$ 5,17"

// MODO PORÇÃO:
// - "1 Porção de venda: 100 g"
// - "Custo por porção: R$ 2,00"
```

### 2. PDF - Tabela de Composição com Colunas de Peso

#### Arquivo: `PdfService.cs` (Linhas ~555-625)

**Novas colunas adicionadas:**
1. **Peso/UN (g)**: Mostra `PesoPorUnidadeGml` quando item é UN
2. **Peso item (g/ml)**: Mostra `PesoItemGml` calculado para cada item

**Lógica de exibição:**
```csharp
// Peso por unidade (apenas para itens UN)
var pesoPorUnDisplay = item.PesoPorUnidadeGml.HasValue 
    ? $"{item.PesoPorUnidadeGml.Value:F2} g"
    : "-";

// Peso total do item (sempre calculado quando possível)
var pesoItemDisplay = item.PesoItemGml > 0
    ? $"{item.PesoItemGml:F2}"
    : "-";
```

**Rodapé da tabela:**
- Soma `PesoItemGml` de todos os itens
- Exibe total em g/ml ou "—" se não houver peso calculado
- **NUNCA mostra "1 g" como fallback**

### 3. Exemplo: Hot Dog no PDF

```
🍽 Venda por Unidade
┌──────────────────────────────────────────────────┐
│ 1 Unidade = Hot Dog                              │
│ Custo por unidade: R$ 5,17                       │
│ Markup mesa: 5.00                                │
│ Preço mesa: R$ 25,85                             │
└──────────────────────────────────────────────────┘

🥘 Composição (Ingredientes/Componentes)
┌───┬──────┬──────────┬────────┬─────────┬───────────┬───────┬─────┐
│ # │ Tipo │ Item     │ Qtd    │ Peso/UN │ Peso item │ Custo │ Obs │
├───┼──────┼──────────┼────────┼─────────┼───────────┼───────┼─────┤
│ 1 │ Ins  │ Pão      │ 1 UN   │ 50.00 g │ 50.00     │ 1,00  │ -   │
│ 2 │ Ins  │ Salsicha │ 1 UN   │ 45.00 g │ 45.00     │ 2,00  │ -   │
│ 3 │ Ins  │ Ketchup  │ 10 GR  │ -       │ 10.00     │ 0,10  │ -   │
│ 4 │ Ins  │ Mostarda │ 10 GR  │ -       │ 10.00     │ 0,08  │ -   │
│ 5 │ Ins  │ Batata   │ 30 GR  │ -       │ 30.00     │ 0,36  │ -   │
├───┼──────┼──────────┼────────┼─────────┼───────────┼───────┼─────┤
│   │      │          │ TOTAL  │         │ 145.00 g  │ 3,54  │     │
└───┴──────┴──────────┴────────┴─────────┴───────────┴───────┴─────┘
```

### 4. Exemplo: Produto sem Peso Calculável

```
Se algum item não tiver peso (UN sem PesoPorUnidade):

💰 Custos e Pesos
┌──────────────────────────────────────────────────┐
│ Custo Total do Lote: R$ 12,00                    │
│ Peso Total Base: —                               │
│ Peso Final (IC/IPC): —                           │
│ Custo/kg final: —                                │
└──────────────────────────────────────────────────┘

🥘 Composição
┌───┬──────┬──────────┬────────┬─────────┬───────────┬───────┐
│ 1 │ Ins  │ Cebola   │ 2 UN   │ —       │ —         │ 1,50  │
│ 2 │ Ins  │ Tomate   │ 200 GR │ —       │ 200.00    │ 4,00  │
├───┼──────┼──────────┼────────┼─────────┼───────────┼───────┤
│   │      │          │ TOTAL  │         │ —         │ 5,50  │
└───┴──────┴──────────┴────────┴─────────┴───────────┴───────┘
```

**Interpretação:** 
- Cebola (UN sem peso) impede cálculo total
- Tomate (GR) tem peso individual calculado
- Total de peso = **"—"** (não inventa "1 g")

## Normalização por Unidade Base (Padrão do Mercado)

### Conceito:
Sistemas profissionais de gestão de receitas normalizam TUDO para uma **unidade base**:
- **Sólidos:** gramas (g)
- **Líquidos:** mililitros (ml)

### Como funciona:

1. **Cadastro de Equivalências** (já implementado via `PesoPorUnidade`):
   ```
   Pão → 1 UN = 50 g
   Salsicha → 1 UN = 45 g
   Ovo → 1 UN = 55 g
   Limão → 1 UN = 60 ml (suco) ou 80 g (polpa)
   ```

2. **Entrada na Receita** (aceita qualquer unidade):
   - 1 UN Pão → converte para 50 g
   - 200 GR Farinha → usa direto 200 g
   - 0.5 KG Açúcar → converte para 500 g

3. **Cálculos Internos** (sempre em g/ml):
   - Peso total = soma de todos os pesos em g/ml
   - Custo por kg = (custo total ÷ peso total g) × 1000
   - Porção = fração do peso total

### Benefícios:
✅ Peso total sempre bate  
✅ Custo por kg/litro faz sentido  
✅ Operação e PDF mostram números coerentes  
✅ Análise nutricional funciona (no futuro)  
✅ Não há mais "1 g inventado"  

### Validação de Negócio:
⚠️ **Bloqueio obrigatório:** Se item é UN e não tem `PesoPorUnidade`, sistema deve:
- Backend: retornar erro ao salvar/validar
- Frontend: mostrar aviso "Configure o peso por unidade do insumo X"
- PDF/Operação: mostrar "—" em vez de valores falsos

## Arquivos Modificados

### Backend (.NET/C#)
- ✅ `src/SGR.Api/Models/Tenant/DTOs/FichaTecnicaItemDto.cs` - Campos `PesoPorUnidadeGml` e `PesoItemGml`
- ✅ `src/SGR.Api/Services/Tenant/Implementations/FichaTecnicaService.cs` - Cálculo de pesos
- ✅ `src/SGR.Api/Services/Common/PdfService.cs` - Modo unidade vs porção + tabela com peso

### Frontend (Angular/TypeScript)
- ✅ `web/src/app/features/tenant-receitas/services/ficha-tecnica.service.ts` - Interfaces
- ✅ `web/src/app/tenant/components/cadastros/ficha-tecnica/ficha-tecnica-form.component.ts` - Helper `formatNumber()`
- ✅ `web/src/app/tenant/components/cadastros/ficha-tecnica/ficha-tecnica-form.component.html` - Colunas de peso

### Documentação
- ✅ `docs/prioridade-vendido-por-unidade.md` - Regras de negócio
- ✅ `docs/correcoes-peso-pdf.md` - Este documento

## Próximos Passos

1. ✅ Compilação backend/frontend sem erros
2. ⏳ **Testar Hot Dog com dados reais**
   - Cadastrar insumos com `PesoPorUnidade`
   - Criar ficha técnica
   - Gerar PDF
   - Validar: Custo por unidade R$ 5,17 → Preço mesa R$ 25,85
3. ⏳ **Testar cenário sem peso**
   - Criar ficha com UN sem `PesoPorUnidade`
   - Verificar se mostra "—" em vez de "1 g"
4. ⏳ Adicionar validação no backend (retornar erro ao salvar UN sem peso)
5. ⏳ Adicionar warning no frontend (avisar usuário antes de salvar)

## Resumo Executivo

### O que estava errado:
- PDF inventava "1 g" quando não sabia calcular peso

### O que foi corrigido:
- PDF agora diferencia modo unidade vs modo porção
- Tabela de composição mostra peso por item e total
- Quando não há peso calculável, mostra "—" (nunca inventa valores)

### Resultado:
- Sistema profissional com normalização por unidade base (g/ml)
- Transparência total sobre pesos e custos
- PDF alinhado com tela de operação
- Pronto para validação com dados reais 🎯
