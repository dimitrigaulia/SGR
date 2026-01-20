# Plano de Execucao - SGR (ajustado com gaps e defaults)

## 1) Resumo do pedido (cliente)
- Cadastro de insumos: informar quantas unidades tem na embalagem (ex: 13 unidades/1kg), usar IPC para custo real por unidade (peca) e ajustar exibicao de custo por g/ml quando fizer sentido.
- Cadastro de receitas: quantidade digitavel/visivel como valor real (ex: 200g, nao 0,200), limitar excesso de casas decimais.
- Ficha tecnica: porcao/peso total somando gr/ml corretos (UN nao vale 1g), conversao UN -> g/ml (ex: pao 50g), markup como fator multiplicativo e Preco Mesa = Custo * Markup, evitar campos duplicados/confusos.
- Validacoes backend: permitir unidade diferente entre item e insumo quando houver conversao (UN com peso por unidade).

## 2) Diagnostico do estado atual

### Implementado
- Modelo/DB: adicionados `UnidadesPorEmbalagem`, `PesoPorUnidade`, `RendimentoPorcoesNumero`, com migration e backfill do texto de rendimento.
  - Arquivos: `src/SGR.Api/Models/Tenant/Entities/Insumo.cs`, `src/SGR.Api/Models/Tenant/Entities/FichaTecnica.cs`, `src/SGR.Api/Migrations/Tenant/20260204120000_AddInsumoUnidadesPesoPorUnidadeAndRendimentoPorcoesNumero.cs`, `src/SGR.Api/Migrations/Tenant/TenantDbContextModelSnapshot.cs`, `src/SGR.Api/Services/Implementations/TenantService.cs`.
- DTOs/API: request/response atualizados para os novos campos, incluindo custo por unidade limpa e aproveitamento.
  - Arquivos: `src/SGR.Api/Models/Tenant/DTOs/CreateInsumoRequest.cs`, `src/SGR.Api/Models/Tenant/DTOs/UpdateInsumoRequest.cs`, `src/SGR.Api/Models/Tenant/DTOs/InsumoDto.cs`, `src/SGR.Api/Models/Tenant/DTOs/CreateFichaTecnicaRequest.cs`, `src/SGR.Api/Models/Tenant/DTOs/UpdateFichaTecnicaRequest.cs`, `src/SGR.Api/Models/Tenant/DTOs/FichaTecnicaDto.cs`.
- Insumos (backend): validacao de campos novos, calculo de custo por unidade limpa e aproveitamento no DTO.
  - Arquivos: `src/SGR.Api/Services/Tenant/Implementations/InsumoService.cs`.
- Ficha tecnica (backend): conversao UN -> g/ml via `PesoPorUnidade`, custo por porcao via `RendimentoPorcoesNumero`, calculo de `CustoKgBase`, validacao de unidade com excecao para UN.
  - Arquivos: `src/SGR.Api/Services/Tenant/Implementations/FichaTecnicaService.cs`, `src/SGR.Api/Controllers/Tenant/FichasTecnicasController.cs`.
- UI Insumos: inputs novos + resumo "custo por unidade limpa".
  - Arquivos: `web/src/app/tenant/components/cadastros/insumo/insumo-form.component.ts`, `web/src/app/tenant/components/cadastros/insumo/insumo-form.component.html`, `web/src/app/features/tenant-insumos/services/insumo.service.ts`.
- UI Receitas: entrada/exibicao de quantidade com conversao (kg/L -> g/mL), arredondamento e step/min ajustados.
  - Arquivos: `web/src/app/tenant/components/cadastros/receita/receita-form.component.ts`, `web/src/app/tenant/components/cadastros/receita/receita-form.component.html`.
- UI Ficha tecnica: conversao de unidades, exibicao de custo kg base/final, suporte a rendimento numerico, ajuste de preco mesa no frontend.
  - Arquivos: `web/src/app/tenant/components/cadastros/ficha-tecnica/ficha-tecnica-form.component.ts`, `web/src/app/tenant/components/cadastros/ficha-tecnica/ficha-tecnica-form.component.html`, `web/src/app/features/tenant-receitas/services/ficha-tecnica.service.ts`.
- PDF/operacao: exibicao de quantidade formatada e resumo de custo por kg base/final.
  - Arquivos: `src/SGR.Api/Services/Common/PdfService.cs`, `web/src/app/tenant/components/operacao/ficha-tecnica-operacao/ficha-tecnica-operacao.component.ts`, `web/src/app/tenant/components/operacao/ficha-tecnica-operacao/ficha-tecnica-operacao.component.html`.
- Testes iniciais para custo por unidade limpa e rendimento/markup.
  - Arquivos: `src/SGR.Tests/InsumoTests.cs`, `src/SGR.Tests/FichaTecnicaServiceTests.cs`, `src/SGR.Tests/SGR.Tests.csproj`, `SGR.slnx`.

### Falta ou pendente (com gaps praticos)
- Gap 1: Definicao exata do "Custo de cada unidade".
  - Recomendacao: adotar (B) `(Embalagem/IPC) * (custo / UnidadesPorEmbalagem)` como custo por unidade (peca) quando houver `UnidadesPorEmbalagem`.
  - "Embalagem" na formula (B) e a quantidade comprada em GR/ML (ex.: 1000g), nao "1 embalagem".
  - Se unidade de compra for KG/L, converter para GR/ML antes de aplicar a formula (B).
  - Manter (A) `custo/IPC * PesoPorUnidade` como custo por g/ml limpo (nao por peca).
  - `PesoPorUnidade` e sempre em GR (ou ML se volume), independente da unidade de compra. Conversao: `pesoTotal = quantidadeEmUN * PesoPorUnidade`.
  - Arquivos alvo: `src/SGR.Api/Models/Tenant/Entities/Insumo.cs`, `src/SGR.Tests/InsumoTests.cs`, `web/src/app/tenant/components/cadastros/insumo/insumo-form.component.ts`.
- Gap 2: ReceitaService para UN com PesoPorUnidade.
  - Regra sugerida: se item esta em UN e o insumo tem PesoPorUnidade, converter custo proporcional ao peso (igual ficha tecnica). Se nao tiver PesoPorUnidade, bloquear/avisar.
  - Arquivos alvo: `src/SGR.Api/Services/Tenant/Implementations/ReceitaService.cs`, `src/SGR.Tests` (novo teste).
- Gap 3: UX de quantidade (200g vs 0,200).
  - Coerencia: persistir em g/mL sempre; exibir g/mL com inteiros ou 0-2 casas max; step=1 para g/mL.
  - Arquivos alvo: `web/src/app/tenant/components/cadastros/receita/receita-form.component.ts`, `web/src/app/tenant/components/cadastros/receita/receita-form.component.html`, `web/src/app/tenant/components/cadastros/ficha-tecnica/ficha-tecnica-form.component.html`.

### Defaults recomendados (decisoes rapidas)
- Insumos:
  - Mostrar "Custo por g/ml" colapsado (toggle "mostrar detalhes") quando `UnidadesPorEmbalagem` estiver preenchido. Default: oculto.
  - `PesoPorUnidade`: unidade sempre GR (ou ML se volume). Auto-preencher apenas se estiver vazio ou se o usuario clicar em "calcular", usando `IPCValor / UnidadesPorEmbalagem`; permitir override.
- Receitas:
  - Persistir quantidades sempre em g/mL (ex.: usuario digita 1,5kg -> salvar 1500g).
  - Input g/mL: step=1 e exibicao sem casas desnecessarias.
- Ficha tecnica:
  - `Custo por Porcao = CustoTotal / RendimentoPorcoesNumero`.
  - `Preco Mesa = Custo por Porcao * Markup`.
  - `Custo por Kg/L = CustoTotal / PesoTotalEmGramas * 1000` (ou mL equivalente).

## 3) Lista de tarefas (ordenada)

- [ ] Custo por unidade limpa: padronizar formula e alinhar testes.
  - Objetivo: adotar (B) `(Embalagem/IPC) * (custo / UnidadesPorEmbalagem)` como fonte de verdade para custo por peca; manter (A) como custo por g/ml limpo.
  - Arquivos provaveis: `src/SGR.Api/Models/Tenant/Entities/Insumo.cs`, `src/SGR.Tests/InsumoTests.cs`.
  - Risco/impacto: baixo (clarifica regra e evita divergencias).
  - Criterios de aceite: teste cobre o exemplo do camarao (custo=100, embalagem=1000, IPC=650, unidades=13) e retorna ~11,83 (2 casas) usando (B); custo por g/ml via (A) segue separado.

- [ ] UX insumos: ocultar custo por g/ml por padrao e criar toggle "mostrar detalhes".
  - Objetivo: reduzir ruido quando o foco e custo por unidade/peca.
  - Arquivos provaveis: `web/src/app/tenant/components/cadastros/insumo/insumo-form.component.ts`, `web/src/app/tenant/components/cadastros/insumo/insumo-form.component.html`.
  - Risco/impacto: baixo (apenas exibicao).
  - Criterios de aceite: se `UnidadesPorEmbalagem` estiver preenchido, custo por g/ml fica colapsado e aparece ao expandir "detalhes".

- [ ] Auto-preencher `PesoPorUnidade` quando possivel.
  - Objetivo: reduzir trabalho manual ao cadastrar insumo comprado por peso.
  - Arquivos provaveis: `web/src/app/tenant/components/cadastros/insumo/insumo-form.component.ts`.
  - Risco/impacto: medio (pode alterar valores ja preenchidos; garantir override).
  - Criterios de aceite: quando `IPCValor` e `UnidadesPorEmbalagem` forem validos, preencher apenas se `PesoPorUnidade` estiver vazio ou se o usuario clicar em "calcular"; manter possibilidade de edicao manual.

- [ ] Receitas: aplicar regra para UN com `PesoPorUnidade` e validar quando faltar.
  - Objetivo: custo de receita coerente com conversao UN -> g/ml e sem erro silencioso.
  - Arquivos provaveis: `src/SGR.Api/Services/Tenant/Implementations/ReceitaService.cs`, `src/SGR.Tests` (novo teste).
  - Risco/impacto: medio (muda custo total/porcao e adiciona validacao).
  - Criterios de aceite: UN com `PesoPorUnidade` converte; UN sem `PesoPorUnidade` retorna erro claro (ex.: "Insumo '{nome}' esta em UN mas nao possui PesoPorUnidade (g/ml). Cadastre o PesoPorUnidade para permitir conversao."); GR/ML inalterado.

- [ ] Padronizar exibicao de quantidades (receita e ficha tecnica).
  - Objetivo: alinhar UI com exibicao "real" (g/mL com inteiros, 0-2 casas no maximo quando necessario).
  - Arquivos provaveis: `web/src/app/tenant/components/cadastros/receita/receita-form.component.ts`, `web/src/app/tenant/components/cadastros/receita/receita-form.component.html`, `web/src/app/tenant/components/cadastros/ficha-tecnica/ficha-tecnica-form.component.html`.
  - Risco/impacto: baixo (apenas exibicao).
  - Criterios de aceite: 200g aparece como 200 (nao 0,200), UN inteiro, sem 4 casas. Padrao unico: moeda com 2 casas; g/ml com 0 casas por padrao (ate 2 casas se < 1 ou houver fracao relevante); UN com 0 casas.

- [ ] Ajustar rotulos de custo na ficha tecnica para separar custos.
  - Objetivo: evitar interpretacao errada do preco mesa vs custo.
  - Arquivos provaveis: `web/src/app/tenant/components/cadastros/ficha-tecnica/ficha-tecnica-form.component.html`.
  - Risco/impacto: baixo.
  - Criterios de aceite: textos coerentes com calculo (Preco Mesa = Custo por Porcao * Markup).

- [ ] Revisar pontos de exibicao em PDF/operacao para manter consistencia com a UI.
  - Objetivo: mesma leitura de quantidade e custo nas saidas.
  - Arquivos provaveis: `src/SGR.Api/Services/Common/PdfService.cs`, `web/src/app/tenant/components/operacao/ficha-tecnica-operacao/ficha-tecnica-operacao.component.html`.
  - Risco/impacto: baixo.
  - Criterios de aceite: quantidades em g/mL/un sem 4 casas, coerentes com o cadastro.

## 4) Notas de migracao
- Ja existe migration para `UnidadesPorEmbalagem`, `PesoPorUnidade`, `RendimentoPorcoesNumero` e backfill de rendimento numerico a partir do texto.
  - Arquivo: `src/SGR.Api/Migrations/Tenant/20260204120000_AddInsumoUnidadesPesoPorUnidadeAndRendimentoPorcoesNumero.cs`.
- Validar se ambientes multi-tenant usam o patch em `TenantService` para schemas existentes.
  - Arquivo: `src/SGR.Api/Services/Implementations/TenantService.cs`.

## 5) Plano de testes (unit + manual)

### Unitarios
- Testar custo por unidade limpa com IPC e unidades por embalagem.
  - Arquivo: `src/SGR.Tests/InsumoTests.cs`.
- Testar ficha tecnica com UN -> g/ml e `RendimentoPorcoesNumero` para custo por porcao.
  - Arquivo: `src/SGR.Tests/FichaTecnicaServiceTests.cs`.
- Se aplicar conversao em receita, criar teste cobrindo UN com `PesoPorUnidade`.
  - Arquivo: `src/SGR.Tests` (novo teste).

### Manuais/E2E
- Insumos:
  - IPC + unidades por embalagem -> custo por unidade limpa.
  - Exibicao de custo por g/ml vs custo por unidade limpa.
- Receitas:
  - Quantidade exibida como 200g (nao 0,200) para unidade KG.
  - Sem excesso de decimais.
- Ficha tecnica:
  - Conversao UN -> g/ml (pao 50g, salsicha 45g).
  - Peso total/porcao soma correta (UN nao equivale 1g).
  - Preco Mesa = Custo por porcao/unidade vendida * Markup.

## 6) Estrategia de rollout
- Incremental sem feature flag (mudanca de calculo e exibicao), monitorar impacto em custos.
- Se necessario, usar flag de exibicao para custo por unidade limpa vs g/ml na UI.
- Garantir compatibilidade com dados antigos (campos novos sao nullaveis e calculos toleram null).

## Proximos passos sugeridos (commits)
- Commit 1: ajustes de UX/labels (insumo + ficha tecnica) e padronizacao de exibicao.
- Commit 2: regra de custo em receita (UN com `PesoPorUnidade`) + testes.
- Commit 3: consistencia PDF/operacao.

## Script para implementacao incremental (opcional)
- Criar e usar `codex_apply_plan.sh` para gerar 3 prompts separados (UX/labels, regra de custo em receita, consistencia PDF/operacao) incluindo `git diff` atual para evitar retrabalho.
