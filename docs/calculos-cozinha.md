# CÃ¡lculos de custo, peso e rendimento da cozinha

Esta documentaÃ§Ã£o explica, em linguagem simples, como o SGR calcula automaticamente custo de ingredientes, custo das receitas, fator de correÃ§Ã£o, fator de rendimento (Ã­ndice de cocÃ§Ã£o) e conversÃ£o de unidades de medida, de forma alinhada a ERPs de cozinha (TOTVS, iFood GestÃ£o, etc.).

## 1. Conceitos bÃ¡sicos

- **Unidade de Compra**  
  Unidade em que o insumo Ã© comprado na nota fiscal.  
  Exemplos: saco 5 kg, garrafa 1 L, dÃºzia de bananas, caixa com 30 ovos, pacote de 1 kg.

- **Unidade de Uso**  
  Unidade usada nas receitas (ficha tÃ©cnica). RecomendaÃ§Ãµes:
  - SÃ³lidos: grama (`g`)
  - LÃ­quidos: mililitro (`mL`)
  - PeÃ§as: unidade (`un`)

- **Unidade Base do Tipo**  
  Cada tipo de unidade (Peso, Volume, Quantidade) tem uma unidade â€œbaseâ€ para os cÃ¡lculos internos:
  - Peso: base em quilograma (`kg`)
  - Volume: base em litro (`L`)
  - Quantidade: base em unidade (`un`)

O sistema usa essas informaÃ§Ãµes para converter automaticamente de â€œunidade de compraâ€ para â€œunidade de usoâ€ e calcular os custos.

## 2. Fator de CorreÃ§Ã£o (Insumos)

Cada `Insumo` possui um campo `FatorCorrecao`, que representa as perdas no preparo individual daquele insumo (limpeza, cascas, aparas, descarte, etc.).

- `1,00` â†’ sem perda (0%)
- `1,10` â†’ cerca de 10% de perdas
- `1,25` â†’ cerca de 25% de perdas

**FÃ³rmula da quantidade bruta do insumo (na unidade de uso):**

```text
QuantidadeBruta = QuantidadeInformadaNaReceita Ã— FatorCorrecao
```

A quantidade informada na receita Ã© sempre na **unidade de uso** do insumo.  
A quantidade bruta Ã© quanto realmente precisa ser retirada do estoque / comprada, jÃ¡ considerando as perdas daquele insumo especÃ­fico.

### Exemplo rÃ¡pido

- Receita usa 1,000 kg de carne â€œlimpaâ€ (quantidade na receita, unidade de uso = kg).
- `FatorCorrecao` da carne = `1,25` (25% de perdas de limpeza).

```text
QuantidadeBruta = 1,000 Ã— 1,25 = 1,250 kg
```

Ou seja, para ter 1 kg limpo, Ã© preciso separar 1,25 kg de carne crua.

## 3. Fator de Rendimento / Ãndice de CocÃ§Ã£o (Receitas)

Cada `Receita` possui um campo `FatorRendimento`, que representa as perdas no preparo da receita como um todo (evaporaÃ§Ã£o, encolhimento na cocÃ§Ã£o, etc.). Ã‰ equivalente ao Ã­ndice de cocÃ§Ã£o de muitos ERPs.

- `< 1,00` â†’ hÃ¡ perdas no preparo (ex.: `0,95` â‰ˆ 5% de perda)
- `1,00` â†’ sem perdas adicionais (padrÃ£o)
- `> 1,00` â†’ ganho de peso (casos raros, mas suportados)

O sistema calcula:

```text
CustoTotalBruto = soma de todos os CustoItem (jÃ¡ com FatorCorrecao dos insumos)
CustoTotal      = CustoTotalBruto / FatorRendimento
CustoPorPorcao  = CustoTotal / Rendimento
```

Onde **Rendimento** Ã© o nÃºmero de porÃ§Ãµes que a receita rende.

### Boas prÃ¡ticas de uso

- Se o **Rendimento** e o **PesoPorPorcao** jÃ¡ consideram as perdas (saÃ­da final pronta), use `FatorRendimento = 1,00`.
- Se quiser modelar as perdas de cocÃ§Ã£o separadamente (Ã­ndice de cocÃ§Ã£o), use um valor menor que 1.  
  Exemplo: receita perde ~5% de peso na cocÃ§Ã£o â†’ `FatorRendimento = 0,95`.

## 4. ConversÃ£o de unidades e cÃ¡lculo de custo dos itens

Cada `Insumo` tem:

- Unidade de compra (ex.: `kg`, `L`, `dz`, `pct`, `cx`)
- Unidade de uso (ex.: `g`, `mL`, `un`)
- `QuantidadePorEmbalagem` (quanto vem na unidade de compra)
- `CustoUnitario` (preÃ§o de uma unidade de compra inteira)

### 4.1. Quando unidade de compra e de uso tÃªm o mesmo tipo

Quando a unidade de compra e a unidade de uso sÃ£o do **mesmo tipo** (`Peso`â€“`Peso`, `Volume`â€“`Volume`, `Quantidade`â€“`Quantidade`) e possuem fatores de conversÃ£o configurados, o sistema faz a conversÃ£o automÃ¡tica:

1. Converte a quantidade da embalagem e a quantidade bruta para a unidade base (ex.: `kg`, `L` ou `un`).
2. Calcula a proporÃ§Ã£o da embalagem utilizada:

   ```text
   ProporcaoEmbalagem = QuantidadeBrutaBase / QuantidadeCompraBase
   ```

3. Calcula o custo do item:

   ```text
   CustoItem = ProporcaoEmbalagem Ã— CustoUnitario
   ```

Esse raciocÃ­nio vale para:

- Peso: `kg` â†” `g`
- Volume: `L` â†” `mL`
- Quantidade: `un` â†” `dz`

### 4.2. Quando unidade de compra e de uso tÃªm tipos diferentes

Se os tipos forem diferentes (por exemplo, pacote â†’ `g`, caixa â†’ `mL`), o sistema **nÃ£o** consegue converter automaticamente a partir dos fatores de unidade. Nesse caso, ele usa um modo simplificado:

```text
CustoItem = (QuantidadeBruta / QuantidadePorEmbalagem) Ã— CustoUnitario
```

Por isso, nesses cenÃ¡rios, Ã© importante que `QuantidadePorEmbalagem` esteja na **mesma unidade de uso** (`g`, `mL` ou `un`) para que o custo fique correto.

## 5. Exemplos prÃ¡ticos

### 5.1. Farinha em saco de 1 kg, usada em g

- Unidade de compra: â€œQuilogramaâ€ (`kg`)
- `QuantidadePorEmbalagem = 1` (1 kg por saco)
- Unidade de uso: â€œGramaâ€ (`g`)
- `CustoUnitario = R$ 6,00` por saco

Receita usa `200 g` de farinha, com `FatorCorrecao = 1,10`:

```text
QuantidadeBruta = 200 g Ã— 1,10 = 220 g
```

Na unidade base (kg):

```text
220 g = 0,220 kg
QuantidadeCompraBase = 1,000 kg
ProporcaoEmbalagem = 0,220 / 1,000 = 0,22
CustoItem = 0,22 Ã— R$ 6,00 = R$ 1,32
```

Ou seja, 200 g lÃ­quidos, com 10% de perdas, consomem 22% de um saco de 1 kg, custando R$ 1,32.

### 5.2. Bananas em dÃºzia, usadas em unidades

- Unidade de compra: â€œDÃºziaâ€ (`dz`), tipo `Quantidade`
- Fator de conversÃ£o: 1 dÃºzia = 12 unidades
- `QuantidadePorEmbalagem = 1` (1 dÃºzia)
- Unidade de uso: â€œUnidadeâ€ (`un`)
- `CustoUnitario = R$ 24,00` por dÃºzia

Receita usa `3 un`, `FatorCorrecao = 1,00`:

```text
QuantidadeBruta = 3 un Ã— 1,00 = 3 un
QuantidadeCompraBase = 12 un
ProporcaoEmbalagem = 3 / 12 = 0,25
CustoItem = 0,25 Ã— R$ 24,00 = R$ 6,00
```

### 5.3. Receita completa com fatores

Para uma receita qualquer:

1. Para cada item da receita:
   - Calcula `QuantidadeBruta` usando `FatorCorrecao` do insumo.
   - Calcula `CustoItem` com base na unidade de compra/uso e na proporÃ§Ã£o da embalagem.
2. Soma todos os `CustoItem` â†’ `CustoTotalBruto`.
3. Aplica o `FatorRendimento` da receita:

   ```text
   CustoTotal = CustoTotalBruto / FatorRendimento
   CustoPorPorcao = CustoTotal / Rendimento
   ```

Assim, o SGR se comporta como os ERPs de cozinha mais conhecidos: vocÃª informa como compra e como usa os insumos, e o sistema cuida automaticamente das conversÃµes, fatores de correÃ§Ã£o, rendimento e custos.

