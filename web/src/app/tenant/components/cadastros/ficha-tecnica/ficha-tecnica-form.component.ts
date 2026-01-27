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
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatChipsModule } from '@angular/material/chips';
import { ToastService } from '../../../../core/services/toast.service';
import { environment } from '../../../../../environments/environment';
import { CategoriaReceitaService, CategoriaReceitaDto } from '../../../../features/tenant-categorias-receita/services/categoria-receita.service';
import { ReceitaService, ReceitaDto } from '../../../../features/tenant-receitas/services/receita.service';
import { InsumoService, InsumoDto } from '../../../../features/tenant-insumos/services/insumo.service';
import { UnidadeMedidaService, UnidadeMedidaDto } from '../../../../features/tenant-unidades-medida/services/unidade-medida.service';
import { FichaTecnicaService, CreateFichaTecnicaRequest, UpdateFichaTecnicaRequest, FichaTecnicaItemDto, FichaTecnicaDto } from '../../../../features/tenant-receitas/services/ficha-tecnica.service';
import { CanalVendaService, CanalVendaDto } from '../../../../features/tenant-canais-venda/services/canal-venda.service';

type FichaTecnicaCanalFormModel = {
  id?: number | null;
  canalVendaId?: number | null;
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
  imports: [CommonModule, FormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule, MatSelectModule, MatSlideToggleModule, MatSnackBarModule, MatTableModule, MatIconModule, MatCardModule, MatCheckboxModule, MatTooltipModule, MatTabsModule, MatProgressSpinnerModule, MatToolbarModule, MatChipsModule],
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
  private canalVendaService = inject(CanalVendaService);
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
  canaisVenda = signal<CanalVendaDto[]>([]);
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
    indiceContabil: 1.0 as number | null,
    icOperador: null as string | null,
    icValor: null as number | null,
    ipcValor: null as number | null,
    margemAlvoPercentual: null as number | null,
    porcaoVendaQuantidade: null as number | null,
    porcaoVendaUnidadeMedidaId: null as number | null,
    rendimentoPorcoesNumero: null as number | null,
    // UI-only selector to choose how the product is sold in cadastro
    formaVenda: 'porcao' as 'porcao' | 'unidade',
    tempoPreparo: null as number | null,
    isAtivo: true
  };

  itens = signal<FichaTecnicaItemFormModel[]>([]);
  canais = signal<FichaTecnicaCanalFormModel[]>([]);
  displayedColumnsItens = ['ordem', 'tipo', 'item', 'quantidade', 'unidade', 'qb', 'observacoes', 'acoes'];
  displayedColumns = ['canal', 'nomeExibicao', 'precoVenda', 'taxas', 'porcentagem', 'acoes'];
  
  // Propriedades para uso no template
  environment = environment;

  // Métodos auxiliares de cálculo
  private calcularCustoPorUnidadeUso(insumo: InsumoDto): number {
    if (insumo.custoUnitario <= 0 || insumo.quantidadePorEmbalagem <= 0) {
      return 0;
    }
    
    // Se IPC informado, usar: CustoUnitario / IPCValor
    // IPC representa quantidade aproveitável na mesma unidade de uso
    if (insumo.ipcValor && insumo.ipcValor > 0) {
      return insumo.custoUnitario / insumo.ipcValor;
    }
    
    // Se IPC não informado, calcular custo por unidade de compra
    return insumo.custoUnitario / insumo.quantidadePorEmbalagem;
  }

  private calcularCustoItem(item: FichaTecnicaItemFormModel): number {
    if (item.quantidade <= 0) return 0;

    if (item.tipoItem === 'Insumo' && item.insumoId) {
      const insumo = this.insumos().find(i => i.id === item.insumoId);
      if (insumo) {
        const custoPorUnidadeUso = this.calcularCustoPorUnidadeUso(insumo);
        const unidade = this.unidades().find(u => u.id === item.unidadeMedidaId);
        const sigla = unidade?.sigla?.toUpperCase();

        if (sigla === 'UN') {
          if (insumo.pesoPorUnidade && insumo.pesoPorUnidade > 0) {
            const pesoPorUnidade = this.ajustarPesoPorUnidade(insumo.pesoPorUnidade, insumo.unidadeCompraSigla);
            return item.quantidade * pesoPorUnidade * custoPorUnidadeUso;
          }
          return 0;
        }

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
    // Considerar itens cuja UnidadeMedida.Sigla seja "GR" ou "ML" (igual ao backend)
    let quantidadeTotalBase = 0;

    for (const item of this.itens()) {
      if (!item.unidadeMedidaId || item.quantidade <= 0) continue;

      const unidade = this.unidades().find(u => u.id === item.unidadeMedidaId);
      if (!unidade) continue;

      const siglaUnidade = unidade.sigla.toUpperCase();

      // IMPORTANTE: Processar receitas primeiro, independente da unidade (usando PesoPorPorcao)
      if (item.tipoItem === 'Receita' && item.receitaId) {
        // Para receitas: sempre usar PesoPorPorcao, independente da unidade (GR ou UN)
        const receita = this.receitas().find(r => r.id === item.receitaId);
        if (receita && receita.pesoPorPorcao && receita.pesoPorPorcao > 0) {
          quantidadeTotalBase += item.quantidade * receita.pesoPorPorcao;
        }
      } else if (item.tipoItem === 'Insumo') {
        // Para insumos: somar quantidade diretamente se GR/ML, ou converter de UN via PesoPorUnidade
        if (siglaUnidade === 'GR' || siglaUnidade === 'ML') {
          quantidadeTotalBase += item.quantidade;
        } else if (siglaUnidade === 'KG' || siglaUnidade === 'L') {
          quantidadeTotalBase += item.quantidade * 1000;
        } else if (siglaUnidade === 'UN') {
          const insumo = this.insumos().find(i => i.id === item.insumoId);
          if (insumo?.pesoPorUnidade && insumo.pesoPorUnidade > 0) {
            // Ajustar PesoPorUnidade se insumo foi comprado em KG/L
            const pesoPorUnidade = this.ajustarPesoPorUnidade(insumo.pesoPorUnidade, insumo.unidadeCompraSigla);
            quantidadeTotalBase += item.quantidade * pesoPorUnidade;
          }
        }
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

  // Renamed to clarify this is cost per gr/mL (equivalência)
  get custoPorGmlCalculado(): number {
    const rendimentoFinal = this.rendimentoFinalCalculado;
    if (rendimentoFinal !== null && rendimentoFinal > 0) {
      return Math.round((this.custoTotalCalculado / rendimentoFinal) * 10000) / 10000;
    }
    return 0;
  }

  // Helper used by template to decide composition headers when there are Receitas
  get hasReceitaItems(): boolean {
    const dto = this.fichaDtoCompleto();
    return !!(dto && dto.itens && dto.itens.some((i: any) => i.tipoItem === 'Receita'));
  }

  get hasPorcao(): boolean {
    const dto = this.fichaDtoCompleto();
    return (this.model.porcaoVendaQuantidade !== null && this.model.porcaoVendaQuantidade > 0) || 
           (dto !== null && dto.porcaoVendaQuantidade !== null && dto.porcaoVendaQuantidade !== undefined && dto.porcaoVendaQuantidade > 0);
  }

  get precoMesaCalculado(): number | null {
    const dto = this.fichaDtoCompleto();
    
    // Ordem deterministica: se porcao definida -> NUNCA calcular no frontend, so usar DTO
    if (this.hasPorcao) {
      // Se porcao definida -> retornar valor do DTO ou null (exibir "-")
      return dto?.precoMesaSugerido ?? null;
    }
    
    // Se sem porcao, priorizar rendimentoPorcoesNumero (produto vendido por unidade)
    const rendimentoPorcoesNumero = this.model.rendimentoPorcoesNumero ?? dto?.rendimentoPorcoesNumero ?? null;
    if (rendimentoPorcoesNumero && rendimentoPorcoesNumero > 0) {
      const custoTotal = this.custoTotalCalculado;
      if (this.model.indiceContabil && this.model.indiceContabil > 0 && custoTotal > 0) {
        const custoPorUnidadeVendida = custoTotal / rendimentoPorcoesNumero;
        return Math.round(custoPorUnidadeVendida * this.model.indiceContabil * 10000) / 10000;
      }
      return null;
    }

    // Se sem porcao e sem rendimentoPorcoesNumero -> legado (pode calcular no frontend)
    const rendimentoFinal = this.rendimentoFinalCalculado;
    if (rendimentoFinal === null || rendimentoFinal <= 0) {
      return null;
    }

    const custoPorUnidade = this.custoPorGmlCalculado;
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

    // Carregar canais de venda
    this.canalVendaService.list({ pageSize: 1000 })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.canaisVenda.set(res.items.filter(c => c.isAtivo));
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
      initialTab = 0; // View mode: Resumo sempre index 0 (única tab)
    } else if (tabParam === 'resumo') {
      initialTab = 1; // Edit mode: Cadastro=0, Resumo=1
    } else {
      initialTab = 0; // Edit mode default: abre no Cadastro
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

  formatNumber(value: number): string {
    return new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 0, maximumFractionDigits: 2 }).format(value || 0);
  }

  // Getters para exibição de Receita e Insumo nos hints
  getReceitaItem(item: FichaTecnicaItemFormModel): any {
    if (item.tipoItem === 'Receita' && item.receitaId) {
      return this.receitas().find(r => r.id === item.receitaId);
    }
    return null;
  }

  getInsumoItem(item: FichaTecnicaItemFormModel): any {
    if (item.tipoItem === 'Insumo' && item.insumoId) {
      return this.insumos().find(i => i.id === item.insumoId);
    }
    return null;
  }

  // Public wrapper para ajustarPesoPorUnidade (usado no template)
  ajustarPesoPorUnidadePublic(pesoPorUnidade: number, unidadeCompraSigla?: string | null): number {
    return this.ajustarPesoPorUnidade(pesoPorUnidade, unidadeCompraSigla);
  }

  // Callback para mudanças de unidade (para recálculos)
  onUnidadeChange(item: FichaTecnicaItemFormModel): void {
    // Força recálculo de componentes que dependem da unidade
    this.itens.set([...this.itens()]);
    this.cdr.markForCheck();
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
      if (insumo) {
        // Defaultar para UN se o insumo tem PesoPorUnidade ou UnidadesPorEmbalagem
        const temPesoPorUnidade = insumo.pesoPorUnidade && insumo.pesoPorUnidade > 0;
        const temUnidadesPerEmbalagem = insumo.unidadesPorEmbalagem && insumo.unidadesPorEmbalagem > 0;
        
        if (temPesoPorUnidade || temUnidadesPerEmbalagem) {
          const unidadeUn = this.unidades().find(u => u.sigla.toUpperCase() === 'UN');
          if (unidadeUn) {
            item.unidadeMedidaId = unidadeUn.id;
          }
        } else if (insumo?.unidadeCompraId) {
          // Caso contrário, usar unidade de compra
          item.unidadeMedidaId = insumo.unidadeCompraId;
        }
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

  private obterSiglaUnidade(unidadeMedidaId: number | null): string {
    if (!unidadeMedidaId) return '';
    const unidade = this.unidades().find(u => u.id === unidadeMedidaId);
    return unidade ? unidade.sigla : '';
  }

  private obterConversaoUnidade(sigla: string): { fator: number; siglaExibicao: string } {
    const upper = sigla.toUpperCase();
    if (upper === 'KG') return { fator: 1000, siglaExibicao: 'gr' };
    if (upper === 'L') return { fator: 1000, siglaExibicao: 'mL' };
    if (upper === 'GR') return { fator: 1, siglaExibicao: 'gr' };
    if (upper === 'ML') return { fator: 1, siglaExibicao: 'mL' };
    return { fator: 1, siglaExibicao: sigla || '-' };
  }

  private ajustarPesoPorUnidade(pesoPorUnidade: number, unidadeCompraSigla?: string | null): number {
    const sigla = (unidadeCompraSigla || '').trim().toUpperCase();
    if (sigla === 'KG' || sigla === 'L') {
      return pesoPorUnidade / 1000;
    }
    return pesoPorUnidade;
  }

  getUnidadeSigla(unidadeMedidaId: number | null): string {
    const sigla = this.obterSiglaUnidade(unidadeMedidaId);
    if (!sigla) return '-';
    return this.obterConversaoUnidade(sigla).siglaExibicao;
  }

  getQuantidadeExibicao(item: FichaTecnicaItemFormModel): number {
    const sigla = this.obterSiglaUnidade(item.unidadeMedidaId);
    const { fator } = this.obterConversaoUnidade(sigla);
    return item.quantidade * fator;
  }

  onQuantidadeExibicaoChange(item: FichaTecnicaItemFormModel, valor: number): void {
    const sigla = this.obterSiglaUnidade(item.unidadeMedidaId);
    const { fator, siglaExibicao } = this.obterConversaoUnidade(sigla);
    let numero = typeof valor === 'number' ? valor : Number(valor);
    if (Number.isNaN(numero)) {
      numero = 0;
    }
    if (siglaExibicao.toUpperCase() === 'UN') {
      numero = Math.round(numero);
    } else {
      numero = Math.round(numero * 100) / 100;
    }
    item.quantidade = fator > 0 ? numero / fator : numero;
    this.onQuantidadeChange(item);
  }

  getQuantidadeStep(item: FichaTecnicaItemFormModel): number {
    const sigla = this.obterSiglaUnidade(item.unidadeMedidaId).toUpperCase();
    if (sigla === 'UN') {
      return 1;
    }
    return 1;
  }

  getQuantidadeMin(item: FichaTecnicaItemFormModel): number {
    const sigla = this.obterSiglaUnidade(item.unidadeMedidaId).toUpperCase();
    if (sigla === 'UN') {
      return 1;
    }
    return 0.01;
  }

  getUnidadesPermitidas(item: FichaTecnicaItemFormModel): UnidadeMedidaDto[] {
    if (item.tipoItem !== 'Insumo' || !item.insumoId) {
      return this.unidades();
    }

    const insumo = this.insumos().find(i => i.id === item.insumoId);
    if (!insumo) {
      return this.unidades();
    }

    const unidades = this.unidades();
    const unidadeCompra = unidades.find(u => u.id === insumo.unidadeCompraId);
    const unidadeUn = unidades.find(u => u.sigla.toUpperCase() === 'UN');

    if (insumo.pesoPorUnidade && insumo.pesoPorUnidade > 0) {
      const lista = [unidadeCompra, unidadeUn].filter((u): u is UnidadeMedidaDto => !!u);
      return lista.filter((u, index, arr) => arr.findIndex(x => x.id === u.id) === index);
    }

    return unidadeCompra ? [unidadeCompra] : [];
  }


  private formatQuantidadeValor(quantidade: number, sigla: string, incluirUnidade = true): string {
    const { fator, siglaExibicao } = this.obterConversaoUnidade(sigla);
    const valorExibicao = quantidade * fator;
    const casas = siglaExibicao.toUpperCase() == 'UN' ? 0 : 2;
    const valorFormatado = new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 0, maximumFractionDigits: casas }).format(valorExibicao);
    if (!incluirUnidade) {
      return valorFormatado;
    }
    return `${valorFormatado} ${siglaExibicao || '-'}`.trim();
  }

  private formatQuantidadeNumero(quantidade: number, sigla: string): string {
    return this.formatQuantidadeValor(quantidade, sigla, false);
  }

  formatQuantidadeFormItem(item: FichaTecnicaItemFormModel): string {
    if (item.tipoItem === 'Receita') {
      return `${item.quantidade}x`;
    }
    const sigla = this.obterSiglaUnidade(item.unidadeMedidaId);
    return this.formatQuantidadeValor(item.quantidade, sigla);
  }

  formatQuantidadeNumeroForm(item: FichaTecnicaItemFormModel): string {
    if (item.tipoItem === 'Receita') {
      return `${item.quantidade}`;
    }
    const sigla = this.obterSiglaUnidade(item.unidadeMedidaId);
    return this.formatQuantidadeNumero(item.quantidade, sigla);
  }

  formatQuantidadeExibicao(valor: number | null | undefined, sigla: string): string {
    if (valor === null || valor === undefined) {
      return '-';
    }
    return this.formatQuantidadeValor(valor, sigla);
  }

  formatQuantidadeItem(item: { tipoItem: string; quantidade: number; unidadeMedidaSigla?: string | null }): string {
    if (item.tipoItem === 'Receita') {
      return `${item.quantidade}x`;
    }
    return this.formatQuantidadeValor(item.quantidade, item.unidadeMedidaSigla ?? '');
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
      return { unidade: 'gr/ml', unidadePreco: 'R$/kg ou L' };
    }
    
    // Se unidades() ainda não carregou, retornar fallback
    if (this.unidades().length === 0) {
      return { unidade: 'gr/ml', unidadePreco: 'R$/kg ou L' };
    }
    
    // Buscar a unidade na lista unidades() e obter a sigla
    const unidade = this.unidades().find(u => u.id === unidadeId);
    if (!unidade) {
      return { unidade: 'gr/ml', unidadePreco: 'R$/kg ou L' };
    }
    
    const sigla = unidade.sigla.toUpperCase();
    if (sigla === 'GR') {
      return { unidade: 'gr', unidadePreco: 'R$/kg' };
    } else if (sigla === 'ML') {
      return { unidade: 'mL', unidadePreco: 'R$/L' };
    }
    
    // Fallback final
    return { unidade: 'gr/ml', unidadePreco: 'R$/kg ou L' };
  }

  isUnidadeTravada(item: FichaTecnicaItemFormModel): boolean {
    // Unidade est? travada se:
    // - Item ? do tipo Receita (sempre GR)
    // - Item ? do tipo Insumo sem convers?o por unidade
    if (item.tipoItem === 'Receita') {
      return true;
    }
    if (item.tipoItem === 'Insumo' && item.insumoId != null) {
      const insumo = this.insumos().find(i => i.id === item.insumoId);
      if (insumo?.pesoPorUnidade && insumo.pesoPorUnidade > 0) {
        return false;
      }
      return true;
    }
    return false;
  }


  addCanal() {
    const current = this.canais();
    this.canais.set([...current, {
      canalVendaId: null,
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

  onCanalVendaSelected(index: number, canalVendaId: number | null) {
    if (!canalVendaId) return;
    
    const canalVenda = this.canaisVenda().find(c => c.id === canalVendaId);
    if (!canalVenda) return;

    const current = this.canais();
    const updated = [...current];
    updated[index] = {
      ...updated[index],
      canalVendaId: canalVenda.id,
      canal: canalVenda.nome,
      nomeExibicao: canalVenda.nome,
      taxaPercentual: canalVenda.taxaPercentualPadrao ?? updated[index].taxaPercentual,
      comissaoPercentual: null,
      multiplicador: null
    };
    this.canais.set(updated);
    this.cdr.markForCheck();
  }

  addCanalPreset(tipo: 'IFOOD1' | 'IFOOD2' | 'BALCAO' | 'DELIVERY') {
    // Tentar encontrar canal de venda pelo nome
    const nomeMap: Record<string, string> = {
      IFOOD1: 'iFood 1',
      IFOOD2: 'iFood 2',
      BALCAO: 'Balcão',
      DELIVERY: 'Delivery Próprio'
    };
    
    const nome = nomeMap[tipo];
    const canalVenda = this.canaisVenda().find(c => c.nome === nome);
    
    const current = this.canais();
    if (canalVenda) {
      // Usar canal de venda cadastrado
      this.canais.set([...current, {
        canalVendaId: canalVenda.id,
        canal: canalVenda.nome,
        nomeExibicao: canalVenda.nome,
        precoVenda: 0,
        taxaPercentual: canalVenda.taxaPercentualPadrao ?? null,
        comissaoPercentual: null,
        multiplicador: null,
        margemCalculadaPercentual: null,
        observacoes: '',
        isAtivo: true
      }]);
    } else {
      // Fallback para presets hardcoded se canal não encontrado
      const presets: Record<string, { canal: string; nomeExibicao: string; taxaPercentual?: number | null }> = {
        IFOOD1: { canal: 'iFood 1', nomeExibicao: 'iFood 1', taxaPercentual: 13 },
        IFOOD2: { canal: 'iFood 2', nomeExibicao: 'iFood 2', taxaPercentual: 25 },
        BALCAO: { canal: 'Balcão', nomeExibicao: 'Balcão', taxaPercentual: 0 },
        DELIVERY: { canal: 'Delivery Próprio', nomeExibicao: 'Delivery Próprio', taxaPercentual: 0 }
      };
      const preset = presets[tipo];
      this.canais.set([...current, {
        canalVendaId: null,
        canal: preset.canal,
        nomeExibicao: preset.nomeExibicao,
        precoVenda: 0,
        taxaPercentual: preset.taxaPercentual ?? null,
        comissaoPercentual: null,
        multiplicador: null,
        margemCalculadaPercentual: null,
        observacoes: '',
        isAtivo: true
      }]);
    }
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

    // Validação: indiceContabil é obrigatório e deve ser maior que zero
    if (!v.indiceContabil || v.indiceContabil <= 0) {
      this.toast.error('Informe o Markup (deve ser maior que zero)');
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
        indiceContabil: v.indiceContabil!,
        icOperador: v.icOperador || undefined,
        icValor: v.icValor ?? undefined,
        ipcValor: v.ipcValor ?? undefined,
        margemAlvoPercentual: v.margemAlvoPercentual ?? undefined,
        porcaoVendaQuantidade: v.porcaoVendaQuantidade ?? undefined,
        porcaoVendaUnidadeMedidaId: v.porcaoVendaUnidadeMedidaId ?? undefined,
        rendimentoPorcoesNumero: v.rendimentoPorcoesNumero ?? undefined,
        tempoPreparo: v.tempoPreparo ?? undefined,
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
          id: c.id ?? undefined,
          canalVendaId: c.canalVendaId ?? undefined,
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
            const msg = err.error?.message || 'Erro ao salvar ficha técnica';
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
        indiceContabil: v.indiceContabil!,
        icOperador: v.icOperador || undefined,
        icValor: v.icValor ?? undefined,
        ipcValor: v.ipcValor ?? undefined,
        margemAlvoPercentual: v.margemAlvoPercentual ?? undefined,
        porcaoVendaQuantidade: v.porcaoVendaQuantidade ?? undefined,
        porcaoVendaUnidadeMedidaId: v.porcaoVendaUnidadeMedidaId ?? undefined,
        rendimentoPorcoesNumero: v.rendimentoPorcoesNumero ?? undefined,
        tempoPreparo: v.tempoPreparo ?? undefined,
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
          canalVendaId: c.canalVendaId ?? undefined,
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
            const msg = err.error?.message || 'Erro ao salvar ficha técnica';
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

    this.router.navigate(['/tenant/fichas-tecnicas', currentId, 'operacao'], { queryParams: { tab } });
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
            indiceContabil: e.indiceContabil ?? 1.0,
            icOperador: e.icOperador ?? null,
            icValor: e.icValor ?? null,
            ipcValor: e.ipcValor ?? null,
            margemAlvoPercentual: e.margemAlvoPercentual ?? null,
            porcaoVendaQuantidade: e.porcaoVendaQuantidade ?? null,
            porcaoVendaUnidadeMedidaId: e.porcaoVendaUnidadeMedidaId ?? null,
            rendimentoPorcoesNumero: e.rendimentoPorcoesNumero ?? null,
            tempoPreparo: e.tempoPreparo ?? null,
            // set formaVenda based on DTO content so model is fully populated
            formaVenda: (e.porcaoVendaQuantidade && e.porcaoVendaQuantidade > 0) ? 'porcao' : 'unidade',
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
            canalVendaId: c.canalVendaId ?? null,
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

  getModoCanal(canal: FichaTecnicaCanalFormModel | any): 'Preço definido' | 'Auto (Gross-up)' | '-' {
    // Se existe taxa percentual, usar gross-up
    if (canal.taxaPercentual && canal.taxaPercentual > 0) {
      return 'Auto (Gross-up)';
    }
    // Se preço está definido manualmente
    if (canal.precoVenda > 0) {
      return 'Preço definido';
    }
    return '-';
  }

  getPorcoesEstimadasOperacao(): string {
    const dto = this.fichaDtoCompleto();
    if (!dto) return '—';

    const pesoFinal = dto.rendimentoFinal ?? null;
    const porcaoQtd = dto.porcaoVendaQuantidade ?? null;

    if (!pesoFinal || !porcaoQtd || porcaoQtd <= 0) return '—';

    const estimado = pesoFinal / porcaoQtd;
    if (!Number.isFinite(estimado) || estimado <= 0) return '—';

    return new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 2 }).format(estimado);
  }

  /**
   * Equivalência em g/mL:
   * - Insumo UN: usa insumo.pesoPorUnidade (assumido em g ou mL conforme seu domínio)
   * - Insumo GR/ML: apenas repete o valor como "peso real"
   * - Receita: usa receita.pesoPorPorcao (g) * quantidade(x)
   */
  getEquivalenciaPeso(it: any): string | null {
    // it: item do DTO completo (fichaDtoCompleto().itens)
    // Esperado: tipoItem, quantidade, unidadeMedidaSigla, insumoId, receitaId
    const tipo = (it.tipoItem || '').toString();
    const qtd = Number(it.quantidade ?? 0);
    if (!Number.isFinite(qtd) || qtd <= 0) return null;

    if (tipo === 'Insumo') {
      const sigla = (it.unidadeMedidaSigla || '').toUpperCase();

      if (sigla === 'UN') {
        const insumo = this.insumos().find(x => x.id === it.insumoId);
        const pesoUn = insumo?.pesoPorUnidade ?? null;
        if (!pesoUn || pesoUn <= 0) return null;

        const total = qtd * pesoUn;
        const un = 'gr'; // se você tiver insumo.pesoPorUnidade em mL para líquidos, ajuste aqui.
        const f = (n: number) => new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 2 }).format(n);

        // Ex.: "1 UN = 50 g • Total: 2 UN = 100 g"
        return `1 UN = ${f(pesoUn)} ${un} • Total: ${f(qtd)} UN = ${f(total)} ${un}`;
      }

      // Se já veio em GR/ML, pode exibir como "peso real"
      if (sigla === 'GR' || sigla === 'ML') {
        const unit = sigla === 'ML' ? 'mL' : 'gr';
        const f = (n: number) => new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 2 }).format(n);
        return `Total: ${f(qtd)} ${unit}`;
      }

      return null;
    }

    if (tipo === 'Receita') {
      const receita = this.receitas().find(r => r.id === it.receitaId);
      const pesoPorPorcao = receita?.pesoPorPorcao ?? null; // em g
      if (!pesoPorPorcao || pesoPorPorcao <= 0) return null;

      const total = qtd * pesoPorPorcao;
      const f = (n: number) => new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 2 }).format(n);

      return `1x = ${f(pesoPorPorcao)} g • Total: ${f(qtd)}x = ${f(total)} g`;
    }

    return null;
  }
}
