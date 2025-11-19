# Cálculos de custo, peso e rendimento da cozinha

Esta documentação explica, em linguagem simples, como o SGR calcula automaticamente custo de ingredientes, custo das receitas, fator de correção, fator de rendimento (índice de cocção) e conversão de unidades de medida, de forma alinhada a ERPs de cozinha (TOTVS, iFood Gestão, etc.).

## 1. Conceitos básicos

- **Unidade de Compra**  
  Unidade em que o insumo é comprado na nota fiscal.  
  Exemplos: saco 5 kg, garrafa 1 L, dúzia de bananas, caixa com 30 ovos, pacote de 1 kg.

- **Unidade de Uso**  
  Unidade usada nas receitas (ficha técnica). Recomendações:
  - Sólidos: grama (`g`)
  - Líquidos: mililitro (`mL`)
  - Peças: unidade (`un`)

- **Unidade Base do Tipo**  
  Cada tipo de unidade (Peso, Volume, Quantidade) tem uma unidade “base” para os cálculos internos:
  - Peso: base em quilograma (`kg`)
  - Volume: base em litro (`L`)
  - Quantidade: base em unidade (`un`)

O sistema usa essas informações para converter automaticamente de “unidade de compra” para “unidade de uso” e calcular os custos.

## 2. Fator de Correção (Insumos)

Cada `Insumo` possui um campo `FatorCorrecao`, que representa as perdas no preparo individual daquele insumo (limpeza, cascas, aparas, descarte, etc.).

- `1,00` → sem perda (0%)
- `1,10` → cerca de 10% de perdas
- `1,25` → cerca de 25% de perdas

**Fórmula da quantidade bruta do insumo (na unidade de uso):**

```text
QuantidadeBruta = QuantidadeInformadaNaReceita × FatorCorrecao
```

A quantidade informada na receita é sempre na **unidade de uso** do insumo.  
A quantidade bruta é quanto realmente precisa ser retirada do estoque / comprada, já considerando as perdas daquele insumo específico.

### Exemplo rápido

- Receita usa 1,000 kg de carne “limpa” (quantidade na receita, unidade de uso = kg).
- `FatorCorrecao` da carne = `1,25` (25% de perdas de limpeza).

```text
QuantidadeBruta = 1,000 × 1,25 = 1,250 kg
```

Ou seja, para ter 1 kg limpo, é preciso separar 1,25 kg de carne crua.

## 3. Fator de Rendimento / Índice de Cocção (Receitas)

Cada `Receita` possui um campo `FatorRendimento`, que representa as perdas no preparo da receita como um todo (evaporação, encolhimento na cocção, etc.). É equivalente ao índice de cocção de muitos ERPs.

- `< 1,00` → há perdas no preparo (ex.: `0,95` ≈ 5% de perda)
- `1,00` → sem perdas adicionais (padrão)
- `> 1,00` → ganho de peso (casos raros, mas suportados)

O sistema calcula:

```text
CustoTotalBruto = soma de todos os CustoItem (já com FatorCorrecao dos insumos)
CustoTotal      = CustoTotalBruto / FatorRendimento
CustoPorPorcao  = CustoTotal / Rendimento
```

Onde **Rendimento** é o número de porções que a receita rende.

### Boas práticas de uso

- Se o **Rendimento** e o **PesoPorPorcao** já consideram as perdas (saída final pronta), use `FatorRendimento = 1,00`.
- Se quiser modelar as perdas de cocção separadamente (índice de cocção), use um valor menor que 1.  
  Exemplo: receita perde ~5% de peso na cocção → `FatorRendimento = 0,95`.

## 4. Conversão de unidades e cálculo de custo dos itens

Cada `Insumo` tem:

- Unidade de compra (ex.: `kg`, `L`, `dz`, `pct`, `cx`)
- Unidade de uso (ex.: `g`, `mL`, `un`)
- `QuantidadePorEmbalagem` (quanto vem na unidade de compra)
- `CustoUnitario` (preço de uma unidade de compra inteira)

### 4.1. Quando unidade de compra e de uso têm o mesmo tipo

Quando a unidade de compra e a unidade de uso são do **mesmo tipo** (`Peso`–`Peso`, `Volume`–`Volume`, `Quantidade`–`Quantidade`) e possuem fatores de conversão configurados, o sistema faz a conversão automática:

1. Converte a quantidade da embalagem e a quantidade bruta para a unidade base (ex.: `kg`, `L` ou `un`).
2. Calcula a proporção da embalagem utilizada:

   ```text
   ProporcaoEmbalagem = QuantidadeBrutaBase / QuantidadeCompraBase
   ```

3. Calcula o custo do item:

   ```text
   CustoItem = ProporcaoEmbalagem × CustoUnitario
   ```

Esse raciocínio vale para:

- Peso: `kg` ↔ `g`
- Volume: `L` ↔ `mL`
- Quantidade: `un` ↔ `dz`

### 4.2. Quando unidade de compra e de uso têm tipos diferentes

Se os tipos forem diferentes (por exemplo, pacote → `g`, caixa → `mL`), o sistema **não** consegue converter automaticamente a partir dos fatores de unidade. Nesse caso, ele usa um modo simplificado:

```text
CustoItem = (QuantidadeBruta / QuantidadePorEmbalagem) × CustoUnitario
```

Por isso, nesses cenários, é importante que `QuantidadePorEmbalagem` esteja na **mesma unidade de uso** (`g`, `mL` ou `un`) para que o custo fique correto.

## 5. Exemplos práticos

### 5.1. Farinha em saco de 1 kg, usada em g

- Unidade de compra: “Quilograma” (`kg`)
- `QuantidadePorEmbalagem = 1` (1 kg por saco)
- Unidade de uso: “Grama” (`g`)
- `CustoUnitario = R$ 6,00` por saco

Receita usa `200 g` de farinha, com `FatorCorrecao = 1,10`:

```text
QuantidadeBruta = 200 g × 1,10 = 220 g
```

Na unidade base (kg):

```text
220 g = 0,220 kg
QuantidadeCompraBase = 1,000 kg
ProporcaoEmbalagem = 0,220 / 1,000 = 0,22
CustoItem = 0,22 × R$ 6,00 = R$ 1,32
```

Ou seja, 200 g líquidos, com 10% de perdas, consomem 22% de um saco de 1 kg, custando R$ 1,32.

### 5.2. Bananas em dúzia, usadas em unidades

- Unidade de compra: “Dúzia” (`dz`), tipo `Quantidade`
- Fator de conversão: 1 dúzia = 12 unidades
- `QuantidadePorEmbalagem = 1` (1 dúzia)
- Unidade de uso: “Unidade” (`un`)
- `CustoUnitario = R$ 24,00` por dúzia

Receita usa `3 un`, `FatorCorrecao = 1,00`:

```text
QuantidadeBruta = 3 un × 1,00 = 3 un
QuantidadeCompraBase = 12 un
ProporcaoEmbalagem = 3 / 12 = 0,25
CustoItem = 0,25 × R$ 24,00 = R$ 6,00
```

### 5.3. Receita completa com fatores

Para uma receita qualquer:

1. Para cada item da receita:
   - Calcula `QuantidadeBruta` usando `FatorCorrecao` do insumo.
   - Calcula `CustoItem` com base na unidade de compra/uso e na proporção da embalagem.
2. Soma todos os `CustoItem` → `CustoTotalBruto`.
3. Aplica o `FatorRendimento` da receita:

   ```text
   CustoTotal = CustoTotalBruto / FatorRendimento
   CustoPorPorcao = CustoTotal / Rendimento
   ```

Assim, o SGR se comporta como os ERPs de cozinha mais conhecidos: você informa como compra e como usa os insumos, e o sistema cuida automaticamente das conversões, fatores de correção, rendimento e custos.

