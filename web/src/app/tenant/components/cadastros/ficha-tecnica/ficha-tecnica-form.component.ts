import { Component, ChangeDetectionStrategy, ChangeDetectorRef, DestroyRef, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatTabsModule } from '@angular/material/tabs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ToastService } from '../../../../core/services/toast.service';
import { environment } from '../../../../../environments/environment';
import { CategoriaReceitaService, CategoriaReceitaDto } from '../../../../features/tenant-categorias-receita/services/categoria-receita.service';
import { ReceitaService, ReceitaDto } from '../../../../features/tenant-receitas/services/receita.service';
import { InsumoService, InsumoDto } from '../../../../features/tenant-insumos/services/insumo.service';
import { UnidadeMedidaService, UnidadeMedidaDto } from '../../../../features/tenant-unidades-medida/services/unidade-medida.service';
import { FichaTecnicaService, CreateFichaTecnicaRequest, UpdateFichaTecnicaRequest, FichaTecnicaItemDto, FichaTecnicaDto } from '../../../../features/tenant-receitas/services/ficha-tecnica.service';

type FichaTecnicaCanalFormModel = {
  id?: number | null;
  canal: string;
  nomeExibicao: string;
  precoVenda: number;
  taxaPercentual?: number | null;
  comissaoPercentual?: number | null;
  multiplicador?: number | null;
  margemCalculadaPercentual?: number | null;
  observacoes?: string;
  isAtivo: boolean;
};

type FichaTecnicaItemFormModel = {
  id?: number | null;
  tipoItem: 'Receita' | 'Insumo';
  receitaId?: number | null;
  insumoId?: number | null;
  quantidade: number;
  unidadeMedidaId: number | null;
  exibirComoQB: boolean;
  ordem: number;
  observacoes?: string;
};

@Component({
  standalone: true,
  selector: 'app-tenant-ficha-tecnica-form',
  imports: [CommonModule, FormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule, MatSelectModule, MatSlideToggleModule, MatSnackBarModule, MatTableModule, MatIconModule, MatCardModule, MatCheckboxModule, MatTooltipModule, MatTabsModule, MatProgressSpinnerModule],
  templateUrl: './ficha-tecnica-form.component.html',
  styleUrls: ['./ficha-tecnica-form.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TenantFichaTecnicaFormComponent {
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private categoriaService = inject(CategoriaReceitaService);
  private receitaService = inject(ReceitaService);
  private insumoService = inject(InsumoService);
  private unidadeService = inject(UnidadeMedidaService);
  private fichaService = inject(FichaTecnicaService);
  private toast = inject(ToastService);
  private cdr = inject(ChangeDetectorRef);
  private destroyRef = inject(DestroyRef);
  private breakpointObserver = inject(BreakpointObserver);
  private http = inject(HttpClient);

  id = signal<number | null>(null);
  categorias = signal<CategoriaReceitaDto[]>([]);
  receitas = signal<ReceitaDto[]>([]);
  insumos = signal<InsumoDto[]>([]);
  unidades = signal<UnidadeMedidaDto[]>([]);
  isEdit = computed(() => this.id() !== null);
  isView = signal<boolean>(false);
  isMobile = signal(false);
  error = signal<string>('');
  selectedTabIndex = signal<number>(0);
  fichaDtoCompleto = signal<FichaTecnicaDto | null>(null);
  isLoadingDetalhe = signal<boolean>(false);

  model = {
    categoriaId: null as number | null,
    receitaPrincipalId: null as number | null,
    nome: '',
    codigo: '',
    descricaoComercial: '',
    indiceContabil: null as number | null,
    icOperador: null as string | null,
    icValor: null as number | null,
    ipcValor: null as number | null,
    margemAlvoPercentual: null as number | null,
    porcaoVendaQuantidade: null as number | null,
    porcaoVendaUnidadeMedidaId: null as number | null,
    isAtivo: true
  };

  itens = signal<FichaTecnicaItemFormModel[]>([]);
  canais = signal<FichaTecnicaCanalFormModel[]>([]);
  displayedColumnsItens = ['ordem', 'tipo', 'item', 'quantidade', 'unidade', 'qb', 'observacoes', 'acoes'];
  displayedColumns = ['canal', 'nomeExibicao', 'precoVenda', 'taxas', 'multiplicador', 'margem', 'acoes'];
  
  // Propriedades para uso no template
  window = typeof window !== 'undefined' ? window : null;
  environment = environment;

  // Métodos auxiliares de cálculo
  private calcularCustoPorUnidadeUso(insumo: InsumoDto): number {
    if (insumo.quantidadePorEmbalagem <= 0) {
      return 0;
    }
    return (insumo.custoUnitario / insumo.quantidadePorEmbalagem) * insumo.fatorCorrecao;
  }

  private calcularCustoItem(item: FichaTecnicaItemFormModel): number {
    if (item.quantidade <= 0) return 0;

    if (item.tipoItem === 'Insumo' && item.insumoId) {
      const insumo = this.insumos().find(i => i.id === item.insumoId);
      if (insumo) {
        const custoPorUnidadeUso = this.calcularCustoPorUnidadeUso(insumo);
        return item.quantidade * custoPorUnidadeUso;
      }
    } else if (item.tipoItem === 'Receita' && item.receitaId) {
      const receita = this.receitas().find(r => r.id === item.receitaId);
      if (receita && receita.custoPorPorcao) {
        return item.quantidade * receita.custoPorPorcao;
      }
    }
    return 0;
  }

  // Getters computed para cálculos em tempo real
  get custoTotalCalculado(): number {
    let custoTotal = 0;
    for (const item of this.itens()) {
      if (
        (item.tipoItem === 'Receita' && item.receitaId) ||
        (item.tipoItem === 'Insumo' && item.insumoId)
      ) {
        custoTotal += this.calcularCustoItem(item);
      }
    }
    return Math.round(custoTotal * 10000) / 10000; // Arredondar para 4 casas decimais
  }

  get rendimentoFinalCalculado(): number | null {
    // Considerar apenas itens cuja UnidadeMedida.Sigla seja "GR"
    let quantidadeTotalBase = 0;

    for (const item of this.itens()) {
      if (!item.unidadeMedidaId || item.quantidade <= 0) continue;

      const unidade = this.unidades().find(u => u.id === item.unidadeMedidaId);
      if (!unidade || unidade.sigla.toUpperCase() !== 'GR') continue;

      if (item.tipoItem === 'Receita' && item.receitaId) {
        // Para receitas: quantidade = número de porções, multiplicar por PesoPorPorcao
        const receita = this.receitas().find(r => r.id === item.receitaId);
        if (receita && receita.pesoPorPorcao && receita.pesoPorPorcao > 0) {
          quantidadeTotalBase += item.quantidade * receita.pesoPorPorcao;
        }
      } else if (item.tipoItem === 'Insumo') {
        // Para insumos: somar quantidade diretamente
        quantidadeTotalBase += item.quantidade;
      }
    }

    // Se não houver itens válidos, retornar null
    if (quantidadeTotalBase <= 0) {
      return null;
    }

    // Aplicar IC (Índice de Cocção)
    let pesoAposCoccao = quantidadeTotalBase;
    if (this.model.icOperador && this.model.icValor !== null && this.model.icValor > 0) {
      const icValor = Math.max(0, Math.min(9999, this.model.icValor));
      const icPercentual = icValor / 100;

      if (this.model.icOperador === '+') {
        pesoAposCoccao = quantidadeTotalBase * (1 + icPercentual);
      } else if (this.model.icOperador === '-') {
        pesoAposCoccao = quantidadeTotalBase * (1 - icPercentual);
      }
    }

    // Aplicar IPC (Índice de Partes Comestíveis)
    // Aplicar apenas se ipcValor for > 0 (não aplicar se for 0 ou null)
    let pesoComestivel = pesoAposCoccao;
    if (this.model.ipcValor !== null && this.model.ipcValor > 0) {
      const ipcValor = Math.max(0, Math.min(999, this.model.ipcValor));
      const ipcPercentual = ipcValor / 100;
      pesoComestivel = pesoAposCoccao * ipcPercentual;
    }
    // Se ipcValor for null ou 0, usar pesoAposCoccao diretamente (100% comestível)

    return pesoComestivel > 0 ? pesoComestivel : null;
  }

  get custoPorUnidadeCalculado(): number {
    const rendimentoFinal = this.rendimentoFinalCalculado;
    if (rendimentoFinal !== null && rendimentoFinal > 0) {
      return Math.round((this.custoTotalCalculado / rendimentoFinal) * 10000) / 10000;
    }
    return 0;
  }

  get hasPorcao(): boolean {
    const dto = this.fichaDtoCompleto();
    return (this.model.porcaoVendaQuantidade !== null && this.model.porcaoVendaQuantidade > 0) || 
           (dto !== null && dto.porcaoVendaQuantidade !== null && dto.porcaoVendaQuantidade !== undefined && dto.porcaoVendaQuantidade > 0);
  }

  get precoMesaCalculado(): number | null {
    const dto = this.fichaDtoCompleto();
    
    // Ordem determinística: se porção definida → NUNCA calcular no frontend, só usar DTO
    if (this.hasPorcao) {
      // Se porção definida → retornar valor do DTO ou null (exibir "—")
      return dto?.precoMesaSugerido ?? null;
    }
    
    // Se sem porção → legado (pode calcular no frontend)
    const rendimentoFinal = this.rendimentoFinalCalculado;
    if (rendimentoFinal === null || rendimentoFinal <= 0) {
      return null;
    }

    const custoPorUnidade = this.custoPorUnidadeCalculado;
    if (this.model.indiceContabil && this.model.indiceContabil > 0 && custoPorUnidade > 0) {
      return Math.round(custoPorUnidade * this.model.indiceContabil * 10000) / 10000;
    }
    return null;
  }

  get precoSugeridoVendaCalculado(): number | null {
    // Manter compatibilidade com código existente, mas usar precoMesaCalculado
    return this.precoMesaCalculado;
  }

  constructor() {
    // Detectar mobile
    this.breakpointObserver.observe([Breakpoints.Handset, Breakpoints.TabletPortrait])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.isMobile.set(result.matches);
        this.cdr.markForCheck();
      });

    // Carregar categorias
    this.categoriaService.list({ pageSize: 1000 })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.categorias.set(res.items.filter(c => c.isAtivo));
          this.cdr.markForCheck();
        }
      });

    // Carregar receitas
    this.receitaService.list({ pageSize: 1000 })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.receitas.set(res.items.filter(r => r.isAtivo));
          this.cdr.markForCheck();
        }
      });

    // Carregar insumos
    this.insumoService.list({ pageSize: 1000 })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.insumos.set(res.items.filter(i => i.isAtivo));
          this.cdr.markForCheck();
        }
      });

    // Carregar unidades de medida
    this.unidadeService.list({ pageSize: 1000 })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.unidades.set(res.items.filter(u => u.isAtivo));
          this.cdr.markForCheck();
        }
      });

    const st: any = this.router.getCurrentNavigation()?.extras.state ?? (typeof window !== 'undefined' ? (window as any).history?.state : undefined);
    const id = st?.id as number | undefined;
    const view = !!st?.view;
    this.isView.set(view);

    // Verificar queryParams para tab
    const tabParam = this.route.snapshot.queryParams['tab'];
    let initialTab = 0;
    if (view) {
      initialTab = 1; // View sempre abre no Resumo
    } else if (tabParam === 'resumo') {
      initialTab = 1; // QueryParam resumo abre na Tab Resumo
    }
    this.selectedTabIndex.set(initialTab);

    if (id) {
      this.id.set(id);
      this.loadFichaDetalhe(id);
    }
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value || 0);
  }

  addItem() {
    const current = this.itens();
    const ordem = current.length > 0 ? Math.max(...current.map(i => i.ordem)) + 1 : 1;
    this.itens.set([...current, {
      tipoItem: 'Insumo',
      receitaId: null,
      insumoId: null,
      quantidade: 0,
      unidadeMedidaId: null,
      exibirComoQB: false,
      ordem,
      observacoes: ''
    }]);
    this.cdr.markForCheck();
  }

  removeItem(index: number) {
    const current = this.itens();
    const newItens = current.filter((_, i) => i !== index);
    // Reordenar itens para evitar buracos na ordem (1, 3, 4...)
    newItens.forEach((item, idx) => {
      item.ordem = idx + 1;
    });
    this.itens.set(newItens);
    this.cdr.markForCheck();
  }

  setUnidadeGramas(item: FichaTecnicaItemFormModel) {
    const unidadeGr = this.unidades().find(u => u.sigla.toUpperCase() === 'GR');
    if (unidadeGr) {
      item.unidadeMedidaId = unidadeGr.id;
    }
  }

  onSelectInsumo(item: FichaTecnicaItemFormModel, insumoId: number | null) {
    item.insumoId = insumoId;
    if (insumoId) {
      const insumo = this.insumos().find(i => i.id === insumoId);
      if (insumo?.unidadeUsoId) {
        item.unidadeMedidaId = insumo.unidadeUsoId;
      }
    } else {
      item.unidadeMedidaId = null;
    }
    // Atualizar signal para forçar recálculo dos getters
    this.itens.set([...this.itens()]);
    this.cdr.markForCheck();
  }

  onSelectReceita(item: FichaTecnicaItemFormModel) {
    this.setUnidadeGramas(item);
    // Atualizar signal para forçar recálculo dos getters
    this.itens.set([...this.itens()]);
    this.cdr.markForCheck();
  }

  onItemTipoChange(item: FichaTecnicaItemFormModel) {
    if (item.tipoItem === 'Receita') {
      item.insumoId = null;
      this.setUnidadeGramas(item);
    } else {
      item.receitaId = null;
      item.unidadeMedidaId = null; // Será preenchida ao selecionar insumo
    }
    // Atualizar signal para forçar recálculo dos getters
    this.itens.set([...this.itens()]);
    this.cdr.markForCheck();
  }

  onQuantidadeChange(item: FichaTecnicaItemFormModel) {
    // Atualizar signal para forçar recálculo dos getters
    this.itens.set([...this.itens()]);
    this.cdr.markForCheck();
  }

  onICChange() {
    this.cdr.markForCheck();
  }

  onIndiceContabilChange() {
    this.cdr.markForCheck();
  }

  getItemNome(item: FichaTecnicaItemFormModel): string {
    if (item.tipoItem === 'Receita' && item.receitaId) {
      const receita = this.receitas().find(r => r.id === item.receitaId);
      return receita?.nome || '';
    } else if (item.tipoItem === 'Insumo' && item.insumoId) {
      const insumo = this.insumos().find(i => i.id === item.insumoId);
      return insumo?.nome || '';
    }
    return '';
  }

  getUnidadeSigla(unidadeMedidaId: number | null): string {
    if (!unidadeMedidaId) return '-';
    const unidade = this.unidades().find(u => u.id === unidadeMedidaId);
    return unidade ? unidade.sigla : '-';
  }

  formatQuantidadeItem(item: { tipoItem: string; quantidade: number; unidadeMedidaSigla?: string | null }): string {
    // Se for Receita, exibir como "1x" ou "1 receita" sem unidade
    if (item.tipoItem === 'Receita') {
      return `${item.quantidade}x`;
    }
    // Para Insumo, exibir quantidade + unidade normalmente
    return `${item.quantidade.toFixed(4)} ${item.unidadeMedidaSigla || '-'}`;
  }

  getUnidadePorcao(): { unidade: string; unidadePreco: string } {
    // Fonte única com prioridade definida
    let unidadeId: number | null = null;
    
    // Prioridade 1: model.porcaoVendaUnidadeMedidaId (o que o usuário está editando)
    if (this.model.porcaoVendaUnidadeMedidaId) {
      unidadeId = this.model.porcaoVendaUnidadeMedidaId;
    }
    // Fallback 1: fichaDtoCompleto()?.porcaoVendaUnidadeMedidaId
    else {
      const dto = this.fichaDtoCompleto();
      if (dto?.porcaoVendaUnidadeMedidaId !== null && dto?.porcaoVendaUnidadeMedidaId !== undefined) {
        unidadeId = dto.porcaoVendaUnidadeMedidaId;
      }
    }
    
    // Fallback final: se não encontrou ID, retornar genérico
    if (!unidadeId) {
      return { unidade: 'GR/ML', unidadePreco: 'R$/kg ou L' };
    }
    
    // Se unidades() ainda não carregou, retornar fallback
    if (this.unidades().length === 0) {
      return { unidade: 'GR/ML', unidadePreco: 'R$/kg ou L' };
    }
    
    // Buscar a unidade na lista unidades() e obter a sigla
    const unidade = this.unidades().find(u => u.id === unidadeId);
    if (!unidade) {
      return { unidade: 'GR/ML', unidadePreco: 'R$/kg ou L' };
    }
    
    const sigla = unidade.sigla.toUpperCase();
    if (sigla === 'GR') {
      return { unidade: 'g', unidadePreco: 'R$/kg' };
    } else if (sigla === 'ML') {
      return { unidade: 'mL', unidadePreco: 'R$/L' };
    }
    
    // Fallback final
    return { unidade: 'GR/ML', unidadePreco: 'R$/kg ou L' };
  }

  isUnidadeTravada(item: FichaTecnicaItemFormModel): boolean {
    // Unidade está travada se:
    // - Item é do tipo Receita (sempre GR)
    // - Item é do tipo Insumo e já tem insumo selecionado (usa unidadeUsoId do insumo)
    if (item.tipoItem === 'Receita') {
      return true;
    }
    if (item.tipoItem === 'Insumo' && item.insumoId != null) {
      return true;
    }
    return false;
  }

  addCanal() {
    const current = this.canais();
    this.canais.set([...current, {
      canal: '',
      nomeExibicao: '',
      precoVenda: 0,
      taxaPercentual: null,
      comissaoPercentual: null,
      multiplicador: null,
      margemCalculadaPercentual: null,
      observacoes: '',
      isAtivo: true
    }]);
    this.cdr.markForCheck();
  }

  addCanalPreset(tipo: 'IFOOD1' | 'IFOOD2' | 'BALCAO' | 'DELIVERY') {
    const presets: Record<string, { canal: string; nomeExibicao: string; taxaPercentual?: number | null; comissaoPercentual?: number | null; multiplicador?: number | null }> = {
      IFOOD1: { canal: 'ifood-1', nomeExibicao: 'Ifood 1', taxaPercentual: 13, comissaoPercentual: null, multiplicador: 1.138 },
      IFOOD2: { canal: 'ifood-2', nomeExibicao: 'Ifood 2', taxaPercentual: 25, comissaoPercentual: null, multiplicador: 1.3 },
      BALCAO: { canal: 'BALCAO', nomeExibicao: 'Balcão', taxaPercentual: 0, comissaoPercentual: null, multiplicador: null },
      DELIVERY: { canal: 'DELIVERY', nomeExibicao: 'Delivery Próprio', taxaPercentual: 0, comissaoPercentual: null, multiplicador: null }
    };

    const preset = presets[tipo];
    const current = this.canais();
    this.canais.set([...current, {
      canal: preset.canal,
      nomeExibicao: preset.nomeExibicao,
      precoVenda: 0,
      taxaPercentual: preset.taxaPercentual ?? null,
      comissaoPercentual: preset.comissaoPercentual ?? null,
      multiplicador: preset.multiplicador ?? null,
      margemCalculadaPercentual: null,
      observacoes: '',
      isAtivo: true
    }]);
    this.cdr.markForCheck();
  }

  removeCanal(index: number) {
    const current = this.canais();
    this.canais.set(current.filter((_, i) => i !== index));
    this.cdr.markForCheck();
  }

  save() {
    this.error.set('');
    if (this.isView()) return;

    const v = this.model;
    if (!v.categoriaId || !v.nome) {
      this.toast.error('Selecione uma categoria e informe o nome');
      return;
    }

    // Validação: se porcaoVendaQuantidade > 0, então porcaoVendaUnidadeMedidaId deve estar preenchido
    if (v.porcaoVendaQuantidade && v.porcaoVendaQuantidade > 0 && !v.porcaoVendaUnidadeMedidaId) {
      this.toast.error('Selecione a unidade da porção de venda');
      return;
    }

    const itensValidos = this.itens().filter(i =>
      (
        (i.tipoItem === 'Receita' && i.receitaId != null) ||
        (i.tipoItem === 'Insumo' && i.insumoId != null)
      )
      && i.quantidade > 0
      && !!i.unidadeMedidaId
    );
    if (itensValidos.length === 0) {
      this.toast.error('Adicione pelo menos um item vÃ¡lido');
      return;
    }

    const canaisValidos = this.canais().filter(c => c.canal && c.precoVenda >= 0);
    if (canaisValidos.length === 0) {
      this.toast.error('Selecione pelo menos um preset de canal comercial');
      return;
    }

    if (this.id() === null) {
      const req: CreateFichaTecnicaRequest = {
        categoriaId: v.categoriaId,
        receitaPrincipalId: v.receitaPrincipalId ?? undefined,
        nome: v.nome,
        codigo: v.codigo || undefined,
        descricaoComercial: v.descricaoComercial || undefined,
        indiceContabil: v.indiceContabil ?? undefined,
        icOperador: v.icOperador || undefined,
        icValor: v.icValor ?? undefined,
        ipcValor: v.ipcValor ?? undefined,
        margemAlvoPercentual: v.margemAlvoPercentual ?? undefined,
        porcaoVendaQuantidade: v.porcaoVendaQuantidade ?? undefined,
        porcaoVendaUnidadeMedidaId: v.porcaoVendaUnidadeMedidaId ?? undefined,
        isAtivo: !!v.isAtivo,
        itens: itensValidos.map(i => ({
          tipoItem: i.tipoItem,
          receitaId: i.receitaId ?? undefined,
          insumoId: i.insumoId ?? undefined,
          quantidade: i.quantidade,
          unidadeMedidaId: i.unidadeMedidaId!,
          exibirComoQB: i.exibirComoQB,
          ordem: i.ordem,
          observacoes: i.observacoes || undefined
        })),
        canais: this.canais().filter(c => c.canal && c.precoVenda >= 0).map(c => ({
          canal: c.canal,
          nomeExibicao: c.nomeExibicao || undefined,
          precoVenda: c.precoVenda,
          taxaPercentual: c.taxaPercentual ?? undefined,
          comissaoPercentual: c.comissaoPercentual ?? undefined,
          multiplicador: c.multiplicador ?? undefined,
          observacoes: c.observacoes || undefined,
          isAtivo: !!c.isAtivo
        }))
      };

      this.fichaService.create(req)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (created) => {
            this.toast.success('Ficha técnica criada');
            // Regra operacional: após salvar, navegar para detalhe (GetById) e abrir tab Resumo
            this.id.set(created.id);
            this.selectedTabIndex.set(1); // Abrir tab Resumo
            this.loadFichaDetalhe(created.id); // Recarregar dados completos
          },
          error: err => {
            const msg = err.error?.message || 'Erro ao salvar ficha tÃ©cnica';
            this.toast.error(msg);
            this.error.set(msg);
            this.cdr.markForCheck();
          }
        });
    } else {
      const req: UpdateFichaTecnicaRequest = {
        categoriaId: v.categoriaId,
        receitaPrincipalId: v.receitaPrincipalId ?? undefined,
        nome: v.nome,
        codigo: v.codigo || undefined,
        descricaoComercial: v.descricaoComercial || undefined,
        indiceContabil: v.indiceContabil ?? undefined,
        icOperador: v.icOperador || undefined,
        icValor: v.icValor ?? undefined,
        ipcValor: v.ipcValor ?? undefined,
        margemAlvoPercentual: v.margemAlvoPercentual ?? undefined,
        porcaoVendaQuantidade: v.porcaoVendaQuantidade ?? undefined,
        porcaoVendaUnidadeMedidaId: v.porcaoVendaUnidadeMedidaId ?? undefined,
        isAtivo: !!v.isAtivo,
        itens: itensValidos.map(i => ({
          id: i.id ?? undefined,
          tipoItem: i.tipoItem,
          receitaId: i.receitaId ?? undefined,
          insumoId: i.insumoId ?? undefined,
          quantidade: i.quantidade,
          unidadeMedidaId: i.unidadeMedidaId!,
          exibirComoQB: i.exibirComoQB,
          ordem: i.ordem,
          observacoes: i.observacoes || undefined
        })),
        canais: this.canais().filter(c => c.canal && c.precoVenda >= 0).map(c => ({
          id: c.id ?? undefined,
          canal: c.canal,
          nomeExibicao: c.nomeExibicao || undefined,
          precoVenda: c.precoVenda,
          taxaPercentual: c.taxaPercentual ?? undefined,
          comissaoPercentual: c.comissaoPercentual ?? undefined,
          multiplicador: c.multiplicador ?? undefined,
          observacoes: c.observacoes || undefined,
          isAtivo: !!c.isAtivo
        }))
      };

      this.fichaService.update(this.id()!, req)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.toast.success('Ficha técnica atualizada');
            // Regra operacional: após salvar, navegar para detalhe (GetById) e abrir tab Resumo
            const currentId = this.id()!;
            this.selectedTabIndex.set(1); // Abrir tab Resumo
            this.loadFichaDetalhe(currentId); // Recarregar dados completos
          },
          error: err => {
            const msg = err.error?.message || 'Erro ao salvar ficha tÃ©cnica';
            this.toast.error(msg);
            this.error.set(msg);
            this.cdr.markForCheck();
          }
        });
    }
  }

  printPdf() {
    const currentId = this.id();
    if (!currentId) return;

    this.http.get(`${this.environment.apiUrl}/tenant/fichas-tecnicas/${currentId}/pdf`, {
      responseType: 'blob'
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (blob) => {
          const url = window.URL.createObjectURL(blob);
          const link = window.open(url, '_blank');
          if (link) {
            link.onload = () => window.URL.revokeObjectURL(url);
          }
        },
        error: (err) => {
          this.toast.error('Erro ao gerar PDF');
        }
      });
  }

  openOperacao(tab: 'comercial' | 'producao' = 'comercial') {
    const currentId = this.id();
    if (!currentId) return;

    const url = this.router.serializeUrl(this.router.createUrlTree(
      ['/tenant/fichas-tecnicas', currentId, 'operacao'],
      { queryParams: { tab } }
    ));

    if (this.window) {
      this.window.open(url, '_blank');
    } else {
      this.router.navigate(['/tenant/fichas-tecnicas', currentId, 'operacao'], { queryParams: { tab } });
    }
  }

  loadFichaDetalhe(id: number) {
    this.isLoadingDetalhe.set(true);
    this.fichaService.get(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: e => {
          this.fichaDtoCompleto.set(e);
          this.model = {
            categoriaId: e.categoriaId,
            receitaPrincipalId: e.receitaPrincipalId ?? null,
            nome: e.nome,
            codigo: e.codigo || '',
            descricaoComercial: e.descricaoComercial || '',
            indiceContabil: e.indiceContabil ?? null,
            icOperador: e.icOperador ?? null,
            icValor: e.icValor ?? null,
            ipcValor: e.ipcValor ?? null,
            margemAlvoPercentual: e.margemAlvoPercentual ?? null,
            porcaoVendaQuantidade: e.porcaoVendaQuantidade ?? null,
            porcaoVendaUnidadeMedidaId: e.porcaoVendaUnidadeMedidaId ?? null,
            isAtivo: e.isAtivo
          };
          this.itens.set(e.itens.map(i => ({
            id: i.id,
            tipoItem: i.tipoItem as 'Receita' | 'Insumo',
            receitaId: i.receitaId ?? null,
            insumoId: i.insumoId ?? null,
            quantidade: i.quantidade,
            unidadeMedidaId: i.unidadeMedidaId,
            exibirComoQB: i.exibirComoQB,
            ordem: i.ordem,
            observacoes: i.observacoes || ''
          })));
          this.canais.set(e.canais.map(c => ({
            id: c.id,
            canal: c.canal,
            nomeExibicao: c.nomeExibicao || '',
            precoVenda: c.precoVenda,
            taxaPercentual: c.taxaPercentual ?? null,
            comissaoPercentual: c.comissaoPercentual ?? null,
            multiplicador: c.multiplicador ?? null,
            margemCalculadaPercentual: c.margemCalculadaPercentual ?? null,
            observacoes: c.observacoes || '',
            isAtivo: c.isAtivo
          })));
          this.isLoadingDetalhe.set(false);
          this.cdr.markForCheck();
        },
        error: () => {
          this.isLoadingDetalhe.set(false);
          this.cdr.markForCheck();
        }
      });
  }

  onTabChange(index: number) {
    this.selectedTabIndex.set(index);
    
    // Se mudou para Tab 2 (Resumo Planilha) e não tem dados completos, carregar
    if (index === 1) {
      const currentId = this.id();
      const fichaDto = this.fichaDtoCompleto();
      
      // Verificar se precisa carregar detalhe completo
      if (currentId && (!fichaDto || !fichaDto.itens || fichaDto.itens.length === 0 || 
          fichaDto.pesoTotalBase === null || fichaDto.pesoTotalBase === undefined)) {
        this.loadFichaDetalhe(currentId);
      }
    }
    
    this.cdr.markForCheck();
  }

  goToPlanilhaTab() {
    this.selectedTabIndex.set(1);
    this.onTabChange(1);
  }

  getModoCanal(canal: FichaTecnicaCanalFormModel | any): 'Preço definido' | 'Auto (Multiplicador)' | 'Auto (Gross-up)' | '-' {
    // Hierarquia: multiplicador > gross-up > preço definido
    // Se existem taxa/comissão ou multiplicador, o modo é Auto (mesmo que precoVenda já esteja preenchido)
    if (canal.multiplicador && canal.multiplicador > 0) {
      return 'Auto (Multiplicador)';
    }
    if ((canal.taxaPercentual ?? 0) + (canal.comissaoPercentual ?? 0) > 0) {
      return 'Auto (Gross-up)';
    }
    // Simplificado: como já retornou antes em multiplicador e gross-up, basta checar precoVenda
    // A hierarquia já garante que "Preço definido" só ocorre quando não é Auto
    if (canal.precoVenda > 0) {
      return 'Preço definido';
    }
    return '-';
  }
}
