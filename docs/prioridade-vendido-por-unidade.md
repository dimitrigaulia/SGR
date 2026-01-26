# Implementação: Prioridade "Vendido por Unidade"

## Data: 2025-01-12

## Objetivo
Ajustar o sistema para que fichas técnicas de produtos **vendidos por unidade** (como Hot Dog, Hambúrguer) tenham prioridade sobre produtos vendidos por porção (g/ml).

## Mudanças Implementadas

### 1. Backend - Modelo de Dados

#### Arquivo: `FichaTecnicaItemDto.cs`
**Novos campos adicionados:**
```csharp
public decimal? PesoPorUnidadeGml { get; set; }  // Para insumos UN, peso unitário em g/ml
public decimal PesoItemGml { get; set; }         // Peso total do item em g/ml
```

**Objetivo:** Dar transparência ao peso real dos ingredientes, especialmente para itens vendidos por unidade (UN).

### 2. Backend - Lógica de Negócio

#### Arquivo: `FichaTecnicaService.cs`

**Mudança 1: Inversão de Prioridade (Linhas ~975-990)**
```csharp
// ANTES (ERRADO):
// if (PorcaoVendaQuantidade > 0) { ... }
// else if (RendimentoPorcoesNumero > 0) { ... }

// DEPOIS (CORRETO):
if (ficha.RendimentoPorcoesNumero.HasValue && ficha.RendimentoPorcoesNumero > 0)
{
    // PRIORIDADE: Modo unidade
    custoPorPorcaoVenda = ficha.CustoTotal / ficha.RendimentoPorcoesNumero.Value;
}
else if (ficha.PorcaoVendaQuantidade.HasValue && ficha.PorcaoVendaQuantidade > 0)
{
    // FALLBACK: Modo porção (g/ml)
    var custoUnitPreciso = ficha.CustoTotal / ficha.RendimentoFinal.Value;
    custoPorPorcaoVenda = custoUnitPreciso * ficha.PorcaoVendaQuantidade.Value;
}
```

**Mudança 2: Cálculo dos Campos de Peso (Linhas ~1030-1075)**
```csharp
// Para cada item da ficha, calcula:
// - pesoPorUnidadeGml: peso unitário quando sigla = "UN"
// - pesoItemGml: peso total do item em g/ml

if (sigla == "UN" && insumo.PesoPorUnidade > 0)
{
    var pesoPorUnidade = AjustarPesoPorUnidade(...);
    pesoPorUnidadeGml = pesoPorUnidade;
    pesoItemGml = quantidade * pesoPorUnidade;
}
else if (sigla == "GR" || sigla == "ML")
{
    pesoItemGml = quantidade;
}
else if (sigla == "KG" || sigla == "L")
{
    pesoItemGml = quantidade * 1000m;
}
// Para receitas:
else if (TipoItem == "Receita" && receita.PesoPorPorcao.HasValue)
{
    pesoItemGml = quantidade * receita.PesoPorPorcao.Value;
}
```

### 3. Frontend - Interfaces TypeScript

#### Arquivo: `ficha-tecnica.service.ts`
```typescript
export interface FichaTecnicaItemDto {
  // ... campos existentes ...
  pesoPorUnidadeGml?: number | null;  // Novo
  pesoItemGml: number;                // Novo
}
```

### 4. Frontend - Componente

#### Arquivo: `ficha-tecnica-form.component.ts`
**Novo método helper:**
```typescript
formatNumber(value: number): string {
  return new Intl.NumberFormat('pt-BR', { 
    minimumFractionDigits: 0, 
    maximumFractionDigits: 2 
  }).format(value || 0);
}
```

#### Arquivo: `ficha-tecnica-form.component.html`

**Mudança 1: Card de Precificação no Tab Operação (Resumo)**
```html
<!-- ANTES: Sempre mostrava "Custo por porção" -->
<!-- DEPOIS: Condicional baseado no modo -->

@if (fichaDtoCompleto()?.rendimentoPorcoesNumero && ...) {
  <!-- Modo UNIDADE -->
  <div class="mini-label">1 Unidade =</div>
  <div class="mid">{{ fichaDtoCompleto()!.rendimentoPorcoesNumero }} unidades</div>
  <div class="mini-label mt8">Custo por unidade</div>
  <div class="mid">{{ formatCurrency(custoPorPorcaoVenda) }}</div>
  <div class="mini-label mt8">Preço mesa</div>
  <div class="big">{{ formatCurrency(precoMesaSugerido) }}</div>
} @else {
  <!-- Modo PORÇÃO (g/ml) -->
  <div class="mini-label">Porção</div>
  <div class="mid">{{ formatQuantidadeExibicao(porcaoVendaQuantidade) }}</div>
  <div class="mini-label mt8">Custo por porção</div>
  <div class="mid">{{ formatCurrency(custoPorPorcaoVenda) }}</div>
  ...
}
```

**Mudança 2: Tabela de Composição - Novas Colunas**
```html
<!-- Coluna 1 (Nova): Peso por UN (g) -->
<ng-container matColumnDef="pesoPorUn">
  <th mat-header-cell *matHeaderCellDef>Peso/UN (g)</th>
  <td mat-cell *matCellDef="let item">
    {{ item.pesoPorUnidadeGml ? formatNumber(item.pesoPorUnidadeGml) : '-' }}
  </td>
  <td mat-footer-cell *matFooterCellDef></td>
</ng-container>

<!-- Coluna 2 (Nova): Peso total do item (g/ml) -->
<ng-container matColumnDef="pesoItem">
  <th mat-header-cell *matHeaderCellDef>Peso item (g/ml)</th>
  <td mat-cell *matCellDef="let item">{{ formatNumber(item.pesoItemGml) }}</td>
  <td mat-footer-cell *matFooterCellDef>
    <strong>{{ formatNumber(pesoTotalBase) }}</strong>
  </td>
</ng-container>

<!-- Ordem das colunas atualizada -->
<tr mat-header-row *matHeaderRowDef="['ordem', 'tipo', 'item', 'quantidade', 
    'pesoPorUn', 'pesoItem', 'custoItem', 'observacoes']"></tr>
```

## Regras de Negócio - Recapitulação

### Modo 1: Vendido por Unidade (PRIORIDADE)
- **Trigger:** `rendimentoPorcoesNumero` > 0
- **Cálculo:** `custoPorPorcaoVenda = custoTotal ÷ rendimentoPorcoesNumero`
- **Exemplo:** Hot Dog
  - Custo Total = R$ 5,17
  - Rendimento = 1 unidade
  - **Custo por unidade = R$ 5,17**
  - Markup = 5
  - **Preço Mesa = R$ 25,85**

### Modo 2: Vendido por Porção (FALLBACK)
- **Trigger:** `porcaoVendaQuantidade` > 0 E `rendimentoPorcoesNumero` não informado
- **Cálculo:** `custoPorPorcaoVenda = (custoTotal ÷ rendimentoFinal) × porcaoVendaQuantidade`
- **Exemplo:** Molho de tomate
  - Custo Total = R$ 10,00
  - Rendimento Final = 500 g
  - Porção = 100 g
  - **Custo por porção = (10 ÷ 500) × 100 = R$ 2,00**

## Pesos dos Itens

### Para Insumos:
- **GR / ML:** pesoItemGml = quantidade
- **KG / L:** pesoItemGml = quantidade × 1000
- **UN:** 
  - pesoPorUnidadeGml = insumo.PesoPorUnidade (ajustado)
  - pesoItemGml = quantidade × pesoPorUnidadeGml

### Para Receitas:
- pesoItemGml = quantidade × receita.PesoPorPorcao

### Totalizador:
- **Peso Total Base:** Soma de todos os pesoItemGml (ANTES dos índices IC/IPC)
- **Rendimento Final:** Peso Total Base APÓS aplicação de IC/IPC

## Validação Necessária

### Caso de Teste: Hot Dog
**Setup:**
1. Criar insumos:
   - Pão (UN, peso = 50g, custo = R$ 1,00)
   - Salsicha (UN, peso = 45g, custo = R$ 2,00)
   - Ketchup (GR, custo/kg = R$ 10,00)
   - Mostarda (GR, custo/kg = R$ 8,00)
   - Batata palha (GR, custo/kg = R$ 12,00)

2. Criar ficha:
   - Nome: Hot Dog
   - Composição:
     * 1 UN Pão (R$ 1,00)
     * 1 UN Salsicha (R$ 2,00)
     * 10 GR Ketchup (R$ 0,10)
     * 10 GR Mostarda (R$ 0,08)
     * 30 GR Batata palha (R$ 0,36)
   - **Custo Total = R$ 3,54** (exemplo hipotético - ajustar conforme necessário)
   - rendimentoPorcoesNumero = 1
   - margemAlvoPercentual = 400 (markup de 5x)

3. **Resultados Esperados:**
   - custoPorPorcaoVenda = R$ 3,54
   - precoMesaSugerido = R$ 17,70
   - pesoTotalBase = 145 g (50 + 45 + 10 + 10 + 30)
   - Na tabela de composição:
     * Pão: Peso/UN = 50g, Peso item = 50g
     * Salsicha: Peso/UN = 45g, Peso item = 45g
     * Ketchup: Peso/UN = -, Peso item = 10g
     * Mostarda: Peso/UN = -, Peso item = 10g
     * Batata: Peso/UN = -, Peso item = 30g
     * **Total:** 145g

## Impactos e Compatibilidade

### ✅ Backward Compatible
- Fichas existentes com `porcaoVendaQuantidade` continuam funcionando normalmente
- Modo porção (g/ml) permanece disponível quando `rendimentoPorcoesNumero` não está preenchido

### ⚠️ Atenção
- Fichas que tinham AMBOS os campos preenchidos (`porcaoVendaQuantidade` E `rendimentoPorcoesNumero`) agora priorizarão o modo unidade
- Revisar fichas existentes para garantir que a nomenclatura está correta:
  - "Custo por unidade" vs "Custo por porção"

## Próximos Passos

1. ✅ Testar compilação backend (.NET)
2. ✅ Testar compilação frontend (Angular)
3. ⏳ Testar caso Hot Dog com dados reais
4. ⏳ Atualizar PDF (PdfService.cs) para exibir colunas de peso
5. ⏳ Documentar no README.md
6. ⏳ Treinar usuários sobre os dois modos

## Arquivos Modificados

### Backend (.NET/C#)
- `src/SGR.Api/Models/Tenant/DTOs/FichaTecnicaItemDto.cs`
- `src/SGR.Api/Services/Tenant/Implementations/FichaTecnicaService.cs`

### Frontend (Angular/TypeScript)
- `web/src/app/features/tenant-receitas/services/ficha-tecnica.service.ts`
- `web/src/app/tenant/components/cadastros/ficha-tecnica/ficha-tecnica-form.component.ts`
- `web/src/app/tenant/components/cadastros/ficha-tecnica/ficha-tecnica-form.component.html`

### Documentação
- `docs/prioridade-vendido-por-unidade.md` (este arquivo)
