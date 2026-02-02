import { Component, computed, inject, signal, effect, ChangeDetectionStrategy, ChangeDetectorRef, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
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
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ReceitaService, CreateReceitaRequest, UpdateReceitaRequest, ReceitaDto, CreateReceitaItemRequest, UpdateReceitaItemRequest } from '../../../../features/tenant-receitas/services/receita.service';
import { CategoriaReceitaService, CategoriaReceitaDto } from '../../../../features/tenant-categorias-receita/services/categoria-receita.service';
import { InsumoService, InsumoDto } from '../../../../features/tenant-insumos/services/insumo.service';
import { UnidadeMedidaService, UnidadeMedidaDto } from '../../../../features/tenant-unidades-medida/services/unidade-medida.service';
import { ToastService } from '../../../../core/services/toast.service';
import { UploadService, UploadResponse } from '../../../../features/usuarios/services/upload.service';
import { MatCardModule } from '@angular/material/card';
import { environment } from '../../../../../environments/environment';
import { MatTabsModule } from '@angular/material/tabs';
import { MatAccordion, MatExpansionModule } from '@angular/material/expansion';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatChipsModule } from '@angular/material/chips';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatDividerModule } from '@angular/material/divider';

type ReceitaItemFormModel = {
  insumoId: number | null;
  quantidade: number;
  unidadeMedidaId: number | null;
  exibirComoQB: boolean;
  ordem: number;
  observacoes: string;
  custoItem?: number;
  custoPorUnidadeUso?: number | null;
  custoPor100UnidadesUso?: number | null;
};

@Component({
  standalone: true,
  selector: 'app-tenant-receita-form',
  imports: [CommonModule, FormsModule, MatCardModule, MatTabsModule, MatSidenavModule, MatDividerModule, MatAccordion, MatExpansionModule, MatToolbarModule, MatChipsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule, MatSelectModule, MatSlideToggleModule, MatSnackBarModule, MatTableModule, MatIconModule, MatCheckboxModule, MatTooltipModule],
  templateUrl: './receita-form.component.html',
  styleUrls: ['./receita-form.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TenantReceitaFormComponent {
  private router = inject(Router);
  private service = inject(ReceitaService);
  private categoriaService = inject(CategoriaReceitaService);
  private insumoService = inject(InsumoService);
  private unidadeService = inject(UnidadeMedidaService);
  private toast = inject(ToastService);
  private upload = inject(UploadService);
  private cdr = inject(ChangeDetectorRef);
  private destroyRef = inject(DestroyRef);
  private breakpointObserver = inject(BreakpointObserver);
  private http = inject(HttpClient);

  id = signal<number | null>(null);
  categorias = signal<CategoriaReceitaDto[]>([]);
  insumos = signal<InsumoDto[]>([]);
  unidades = signal<UnidadeMedidaDto[]>([]);
  insumosLoaded = signal(false);
  unidadesLoaded = signal(false);
  receitaLoaded = signal(false);
  isEdit = computed(() => this.id() !== null);
  isView = signal<boolean>(false);
  isMobile = signal(false);
  error = signal<string>('');
  previousImageUrl: string | null = null;
  
  model = {
    nome: '',
    categoriaId: null as number | null,
    descricao: '',
    conservacao: '',
    instrucoesEmpratamento: '',
    rendimento: 1,
    pesoPorPorcao: null as number | null,
    fatorRendimento: 1.0,
    icSinal: '-' as string | null,
    icValor: 0 as number | null,
    tempoPreparo: null as number | null,
    versao: '1.0',
    pathImagem: '',
    isAtivo: true,
    calcularRendimentoAutomatico: false,
    custoTotal: null as number | null,
    custoPorPorcao: null as number | null
  };

  itens = signal<ReceitaItemFormModel[]>([]);
  displayedColumns = ['ordem', 'insumo', 'quantidade', 'unidade', 'qb', 'custoPorUnidadeUso', 'custo', 'observacoes', 'acoes'];
  
  // Propriedades para uso no template
  window = typeof window !== 'undefined' ? window : null;
  environment = environment;

  get fatorRendimentoCalculado(): number {
    const sinal = this.model.icSinal || '-';
    const v = this.model.icValor ?? 0;
    const delta = Math.max(0, Math.min(999, v)) / 100;
    if (delta === 0) return 1.0;
    return sinal === '-' ? 1 - delta : 1 + delta;
  }

  get todosItensEmGramas(): boolean {
    const validItens = this.itens().filter(i =>
      i.insumoId !== null &&
      i.unidadeMedidaId !== null &&
      i.quantidade > 0
    );
    if (validItens.length === 0) return false;

    return validItens.every(i => {
      const unidade = this.unidades().find(u => u.id === i.unidadeMedidaId);
      const sigla = (unidade?.sigla || '').toUpperCase();
      return sigla === 'GR';
    });
  }

  get temItensEmMl(): boolean {
    for (const item of this.itens()) {
      if (!item.unidadeMedidaId || item.quantidade <= 0) continue;

      const unidade = this.unidades().find(u => u.id === item.unidadeMedidaId);
      const sigla = (unidade?.sigla || '').toUpperCase();

      if (sigla === 'ML' || sigla === 'L') return true;

      if (sigla === 'UN') {
        const insumo = this.insumos().find(i => i.id === item.insumoId);
        // Usa fallback: se unidadeCompraSigla vier null, tenta via unidadeCompraId
        const baseSigla = this.obterSiglaUnidadeCompra(insumo);
        if (baseSigla === 'ML' || baseSigla === 'L') return true;
      }
    }
    return false;
  }

  get icValorMax(): number {
    // Se perda (-): máximo 99.99% (evitar 100% que zera)
    // Se ganho (+): máximo 300% (permitir ganhos maiores)
    return this.model.icSinal === '-' ? 99.99 : 300;
  }

  constructor() {
    // Effect para normalizar itens quando todas as dependências estiverem prontas
    effect(() => {
      const insumosOk = this.insumosLoaded();
      const unidadesOk = this.unidadesLoaded();
      const receitaOk = this.receitaLoaded();
      const currentItens = this.itens();
      
      // Só normaliza quando tudo estiver carregado E houver itens
      if (insumosOk && unidadesOk && receitaOk && currentItens.length > 0) {
        // Criar nova array para evitar mutação direta
        const itensNormalizados = currentItens.map(item => ({ ...item }));
        let houveMudanca = false;
        
        itensNormalizados.forEach(item => {
          const siglaAntes = this.obterSiglaUnidade(item.unidadeMedidaId);
          this.normalizarUnidadeItemParaBase(item);
          const siglaDepois = this.obterSiglaUnidade(item.unidadeMedidaId);
          if (siglaAntes !== siglaDepois) {
            houveMudanca = true;
          }
        });
        
        if (houveMudanca) {
          this.itens.set(itensNormalizados);
          this.atualizarCalculosAutomaticos();
          this.atualizarCustosItens();
          this.cdr.markForCheck();
        }
      }
    }, { allowSignalWrites: true });

    // Detectar mobile
    this.breakpointObserver.observe([Breakpoints.Handset, Breakpoints.TabletPortrait])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.isMobile.set(result.matches);
        this.cdr.markForCheck();
      });

    // Carregar categorias e insumos
    this.categoriaService.list({ pageSize: 1000 })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ 
        next: res => {
          this.categorias.set(res.items);
          this.cdr.markForCheck();
        }
      });

    this.insumoService.list({ pageSize: 1000 })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ 
        next: res => {
          this.insumos.set(res.items.filter(i => i.isAtivo));
          this.insumosLoaded.set(true);
          this.cdr.markForCheck();
        }
      });

    this.unidadeService.list({ pageSize: 1000 })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ 
        next: res => {
          this.unidades.set(res.items.filter(u => u.isAtivo));
          this.unidadesLoaded.set(true);
          this.cdr.markForCheck();
        }
      });

    // Ler state (id/view)
    const st: any = this.router.getCurrentNavigation()?.extras.state ?? (typeof window !== 'undefined' ? (window as any).history?.state : undefined);
    const id = st?.id as number | undefined;
    const view = !!st?.view;
    this.isView.set(view);
    if (id) {
      this.id.set(id);
      this.service.get(id)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(e => {
          this.model = { 
            nome: e.nome,
            categoriaId: e.categoriaId,
            descricao: e.descricao || '',
            conservacao: e.conservacao || '',
            instrucoesEmpratamento: e.instrucoesEmpratamento || '',
            rendimento: e.rendimento,
            pesoPorPorcao: e.pesoPorPorcao ?? null,
            fatorRendimento: e.fatorRendimento,
            icSinal: e.icSinal ?? '-',
            icValor: e.icValor ?? 0,
            tempoPreparo: e.tempoPreparo ?? null,
            versao: e.versao || '1.0',
            pathImagem: e.pathImagem || '',
            isAtivo: e.isAtivo,
            calcularRendimentoAutomatico: false,
            custoTotal: e.custoTotal ?? null,
            custoPorPorcao: e.custoPorPorcao ?? null
          };
          this.previousImageUrl = e.pathImagem ?? null;
          this.itens.set(e.itens.map(item => ({
            insumoId: item.insumoId,
            quantidade: item.quantidade,
            unidadeMedidaId: item.unidadeMedidaId,
            exibirComoQB: item.exibirComoQB,
            ordem: item.ordem,
            observacoes: item.observacoes || '',
            custoItem: item.custoItem,
            custoPorUnidadeUso: item.custoPorUnidadeUso ?? null,
            custoPor100UnidadesUso: item.custoPor100UnidadesUso ?? null
          })));
          
          this.receitaLoaded.set(true);
          this.cdr.markForCheck();
        });
    } else {
      // Adicionar um item vazio inicial
      this.addItem();
    }
  }

  addItem() {
    const currentItens = this.itens();
    const newOrdem = currentItens.length > 0 ? Math.max(...currentItens.map(i => i.ordem)) + 1 : 1;
    this.itens.set([...currentItens, {
      insumoId: null,
      quantidade: 0,
      unidadeMedidaId: null,
      exibirComoQB: false,
      ordem: newOrdem,
      observacoes: ''
    }]);
    this.atualizarCalculosAutomaticos();
    this.atualizarCustosItens();
    this.cdr.markForCheck();
  }

  removeItem(index: number) {
    const currentItens = this.itens();
    const newItens = currentItens.filter((_, i) => i !== index);
    // Reordenar
    newItens.forEach((item, idx) => {
      item.ordem = idx + 1;
    });
    this.itens.set(newItens);
    this.atualizarCalculosAutomaticos();
    this.atualizarCustosItens();
    this.cdr.markForCheck();
  }

  moveItemUp(index: number) {
    if (index === 0) return;
    const currentItens = [...this.itens()];
    [currentItens[index - 1], currentItens[index]] = [currentItens[index], currentItens[index - 1]];
    currentItens.forEach((item, idx) => {
      item.ordem = idx + 1;
    });
    this.itens.set(currentItens);
    this.cdr.markForCheck();
  }

  moveItemDown(index: number) {
    const currentItens = this.itens();
    if (index === currentItens.length - 1) return;
    const newItens = [...currentItens];
    [newItens[index], newItens[index + 1]] = [newItens[index + 1], newItens[index]];
    newItens.forEach((item, idx) => {
      item.ordem = idx + 1;
    });
    this.itens.set(newItens);
    this.cdr.markForCheck();
  }

  getInsumoNome(insumoId: number | null): string {
    if (!insumoId) return '';
    const insumo = this.insumos().find(i => i.id === insumoId);
    return insumo ? insumo.nome : '';
  }

  private obterSiglaUnidade(unidadeMedidaId: number | null): string {
    if (!unidadeMedidaId) return '';
    const unidade = this.unidades().find(u => u.id === unidadeMedidaId);
    return unidade ? unidade.sigla : '';
  }

  private getUnidadeIdPorSigla(sigla: string): number | null {
    const u = this.unidades().find(x => (x.sigla || '').toUpperCase() === sigla.toUpperCase());
    return u?.id ?? null;
  }

  private obterSiglaUnidadeCompra(insumo: InsumoDto | undefined): string {
    if (!insumo) return '';
    // Primeiro tenta usar unidadeCompraSigla direto (ja normalizado do backend)
    if (insumo.unidadeCompraSigla) {
      return insumo.unidadeCompraSigla.toUpperCase();
    }
    // Fallback: se unidadeCompraSigla vier null, usa unidadeCompraId para buscar na lista
    return (this.obterSiglaUnidade(insumo.unidadeCompraId ?? null) || '').toUpperCase();
  }

  private normalizarUnidadeItemParaBase(item: ReceitaItemFormModel): void {
    // Legado: receitas antigas podem vir em KG/L/G; normalizar para GR/ML/UN
    const sigla = (this.obterSiglaUnidade(item.unidadeMedidaId) || '').toUpperCase();
    
    // Return rápido se já estiver em unidade base (GR/ML/UN)
    if (sigla === 'GR' || sigla === 'ML' || sigla === 'UN') {
      return;
    }
    
    const qtd = item.quantidade || 0;

    // Converter KG -> GR
    if (sigla === 'KG') {
      const grId = this.getUnidadeIdPorSigla('GR');
      if (grId) {
        item.quantidade = Math.round(qtd * 1000);
        item.unidadeMedidaId = grId;
      }
    }

    // Converter L -> ML
    if (sigla === 'L') {
      const mlId = this.getUnidadeIdPorSigla('ML');
      if (mlId) {
        item.quantidade = Math.round(qtd * 1000);
        item.unidadeMedidaId = mlId;
      }
    }

    // Converter G -> GR (legado)
    if (sigla === 'G') {
      const grId = this.getUnidadeIdPorSigla('GR');
      if (grId) {
        item.quantidade = qtd; // mesma quantidade, só mudar unidade
        item.unidadeMedidaId = grId;
      }
    }
  }

  private obterConversaoUnidade(sigla: string): { fator: number; siglaExibicao: string } {
    const upper = sigla.toUpperCase();
    if (upper === 'GR') return { fator: 1, siglaExibicao: 'g' };
    if (upper === 'ML') return { fator: 1, siglaExibicao: 'mL' };
    // Se chegou KG/L aqui, normalização falhou - exibir como está para evidenciar
    return { fator: 1, siglaExibicao: sigla || '' };
  }

  private conversaoParaBaseCusto(sigla: string): { fator: number; siglaExibicao: string } {
    const upper = (sigla || '').toUpperCase();
    if (upper === 'KG') return { fator: 1000, siglaExibicao: 'g' };
    if (upper === 'L')  return { fator: 1000, siglaExibicao: 'mL' };
    if (upper === 'GR') return { fator: 1, siglaExibicao: 'g' };
    if (upper === 'ML') return { fator: 1, siglaExibicao: 'mL' };
    return { fator: 1, siglaExibicao: sigla || '' };
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
    if (!sigla) return '';
    return this.obterConversaoUnidade(sigla).siglaExibicao;
  }

  getQuantidadeExibicao(item: ReceitaItemFormModel): number {
    return item.quantidade || 0;
  }

  onQuantidadeExibicaoChange(item: ReceitaItemFormModel, valor: number): void {
    let n = typeof valor === 'number' ? valor : Number(valor);
    if (!Number.isFinite(n)) n = 0;

    const sigla = (this.obterSiglaUnidade(item.unidadeMedidaId) || '').toUpperCase();

    if (sigla === 'UN') {
      n = Math.round(n);
      if (n < 1) n = 1;
    } else {
      // GR/ML: inteiro
      n = Math.round(n);
      if (n < 0) n = 0;
    }

    item.quantidade = n;
    
    // Recalcular imediatamente para dar feedback instantâneo
    this.atualizarCalculosAutomaticos();
    this.atualizarCustosItens();
    this.cdr.markForCheck();
  }

  getStepForUnidade(unidadeMedidaId: number | null): string {
    return '1';
  }

  getMinForUnidade(unidadeMedidaId: number | null): number {
    const sigla = (this.obterSiglaUnidade(unidadeMedidaId) || '').toUpperCase();
    return sigla === 'UN' ? 1 : 0;
  }

  formatQuantidade(quantidade: number, unidadeMedidaId: number | null): string {
    const casas = 0;
    return new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 0, maximumFractionDigits: casas }).format(quantidade || 0);
  }

  formatQuantidadeNumero(valor: number | null | undefined, sigla: string): string {
    if (valor === null || valor === undefined) {
      return '-';
    }
    const { fator, siglaExibicao } = this.obterConversaoUnidade(sigla);
    const valorExibicao = valor * fator;
    const casas = siglaExibicao.toUpperCase() == 'UN' ? 0 : 2;
    return new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 0, maximumFractionDigits: casas }).format(valorExibicao);
  }

  onInsumoChange(item: ReceitaItemFormModel, index: number) {
    // Quando o insumo muda, preencher automaticamente a unidade com a unidade de medida do insumo
    if (item.insumoId) {
      const insumo = this.insumos().find(i => i.id === item.insumoId);
      if (insumo) {
        // Defaultar para UN se o insumo tem PesoPorUnidade ou UnidadesPorEmbalagem
        const temPesoPorUnidade = insumo.pesoPorUnidade && insumo.pesoPorUnidade > 0;
        const temUnidadesPerEmbalagem = insumo.unidadesPorEmbalagem && insumo.unidadesPorEmbalagem > 0;
        
        if (temPesoPorUnidade || temUnidadesPerEmbalagem) {
          const unId = this.getUnidadeIdPorSigla('UN');
          if (unId) {
            item.unidadeMedidaId = unId;
          }
        } else if (insumo.unidadeCompraId) {
          item.unidadeMedidaId = insumo.unidadeCompraId;
          this.normalizarUnidadeItemParaBase(item);
        }
      }
    }
    this.onUnidadeMedidaChange();
    this.atualizarCalculosAutomaticos();
    this.atualizarCustosItens();
    this.cdr.markForCheck();
  }

  onUnidadeMedidaChange(): void {
    // Se toggle está ligado e agora não todos itens estão em GR, desligar toggle
    if (this.model.calcularRendimentoAutomatico && !this.todosItensEmGramas) {
      this.model.calcularRendimentoAutomatico = false;
      this.toast.info('Cálculo automático desativado: todos os itens precisam estar em GR.');
      this.cdr.markForCheck();
    }
  }

  onUnidadeMedidaChangeItem(item: ReceitaItemFormModel): void {
    this.normalizarUnidadeItemParaBase(item);

    if (this.model.calcularRendimentoAutomatico && !this.todosItensEmGramas) {
      this.model.calcularRendimentoAutomatico = false;
      this.toast.info('Cálculo automático desativado: todos os itens precisam estar em GR.');
    }

    this.atualizarCalculosAutomaticos();
    this.atualizarCustosItens();
    this.cdr.markForCheck();
  }

  get pesoTotalTeorico(): number | null {
    const pesoPorPorcao = this.model.pesoPorPorcao;
    const rendimento = this.model.rendimento;
    if (pesoPorPorcao && rendimento > 0) {
      return pesoPorPorcao * rendimento;
    }
    return null;
  }

  get pesoTotalItens(): number | null {
    // Somar peso considerando SOMENTE GR e UN via PesoPorUnidade (não incluir ML - volume)
    let total = 0;
    let temItensPeso = false;

    for (const item of this.itens()) {
      if (!item.insumoId || !item.unidadeMedidaId || item.quantidade <= 0) {
        continue;
      }

      const unidade = this.unidades().find(u => u.id === item.unidadeMedidaId);
      if (!unidade) continue;

      const sigla = unidade.sigla.toUpperCase();
      
      // Se é GR, somar direto (massa)
      if (sigla === 'GR') {
        total += item.quantidade;
        temItensPeso = true;
        continue;
      }

      // Se é ML, ignorar (volume, sem densidade)
      if (sigla === 'ML') {
        // volume não entra no peso (sem densidade)
        continue;
      }

      // Se eh UN, usar PesoPorUnidade para converter
      if (sigla === 'UN') {
        const insumo = this.insumos().find(i => i.id === item.insumoId);
        if (insumo && insumo.pesoPorUnidade && insumo.pesoPorUnidade > 0) {
          // Usa fallback: se unidadeCompraSigla vier null, tenta via unidadeCompraId
          const baseSigla = this.obterSiglaUnidadeCompra(insumo);
          // UN que representa volume por unidade (ex.: 50 mL/un) -> nao soma em massa
          if (baseSigla === 'ML' || baseSigla === 'L') {
            continue;
          }
          // Ajustar PesoPorUnidade se o insumo foi comprado em KG/L
          const pesoPorUnidade = this.ajustarPesoPorUnidade(insumo.pesoPorUnidade, insumo.unidadeCompraSigla);
          total += item.quantidade * pesoPorUnidade;
          temItensPeso = true;
        }
        // Se nao tiver PesoPorUnidade, nao somar (avisar ao usuario depois)
      }
    }

    return temItensPeso ? total : null;
  }

  get pesoTotalAposRendimento(): number | null {
    const pesoTotal = this.pesoTotalItens;
    if (pesoTotal === null) return null;
    return pesoTotal * this.fatorRendimentoCalculado;
  }

  calcularPesoPorPorcaoAutomatico(): void {
    if (this.model.calcularRendimentoAutomatico) return; // Não calcular se toggle estiver ativo
    const pesoTotal = this.pesoTotalAposRendimento;
    const rendimento = this.model.rendimento;
    if (pesoTotal !== null && rendimento > 0) {
      this.model.pesoPorPorcao = Math.round(pesoTotal / rendimento * 100) / 100; // Arredondar para 2 casas
      this.cdr.markForCheck();
    }
  }

  calcularRendimentoAutomatico(): void {
    if (!this.model.calcularRendimentoAutomatico) return;
    const pesoTotal = this.pesoTotalAposRendimento;
    const pesoPorPorcao = this.model.pesoPorPorcao;
    if (pesoTotal !== null && pesoPorPorcao && pesoPorPorcao > 0) {
      this.model.rendimento = Math.round(pesoTotal / pesoPorPorcao * 100) / 100; // Arredondar para 2 casas
      this.cdr.markForCheck();
    }
  }

  atualizarCalculosAutomaticos(): void {
    if (this.model.calcularRendimentoAutomatico) {
      this.calcularRendimentoAutomatico();
    } else {
      this.calcularPesoPorPorcaoAutomatico();
    }
  }

  onRendimentoChange(): void {
    if (!this.model.calcularRendimentoAutomatico) {
      this.calcularPesoPorPorcaoAutomatico();
    }
    // Os custos são recalculados automaticamente pelos getters
    this.cdr.markForCheck();
  }

  onPesoPorPorcaoChange(): void {
    if (this.model.calcularRendimentoAutomatico) {
      this.calcularRendimentoAutomatico();
    }
  }

  onICChange(): void {
    this.atualizarCalculosAutomaticos();
    this.atualizarCustosItens();
  }

  onToggleChange(): void {
    // Se tentar ativar sem pré-condição, desfaz e orienta
    if (this.model.calcularRendimentoAutomatico && !this.todosItensEmGramas) {
      this.model.calcularRendimentoAutomatico = false;
      this.toast.error('Cálculo automático de rendimento só funciona quando todos os itens estão em GR.');
      this.cdr.markForCheck();
      return;
    }
    this.atualizarCalculosAutomaticos();
  }

  formatCurrency(value: number | null | undefined): string {
    if (value === null || value === undefined) {
      return '-';
    }
    return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);
  }

  // Preview para UI; fonte da verdade = backend
  calcularCustoPorUnidadeUso(insumo: InsumoDto): number {
    const quantidadePorEmbalagem = insumo.quantidadePorEmbalagem;
    const custo = insumo.custoUnitario || 0;
    const ipcValor = insumo.ipcValor;

    if (custo <= 0) {
      return 0;
    }

    // Se IPC informado, usar: CustoUnitario / IPCValor
    if (ipcValor && ipcValor > 0) {
      return custo / ipcValor;
    }

    // Se IPC não informado, calcular custo por unidade de compra
    if (quantidadePorEmbalagem <= 0) {
      return 0;
    }
    return custo / quantidadePorEmbalagem;
  }

  formatarCustoPorUnidadeUso(item: ReceitaItemFormModel): string {
    if (!item.insumoId) return '-';
    const insumo = this.insumos().find(i => i.id === item.insumoId);
    if (!insumo) return '-';
    
    const custo = this.calcularCustoPorUnidadeUso(insumo);
    const siglaBase = this.obterSiglaUnidade(insumo.unidadeCompraId);
    const { fator, siglaExibicao } = this.conversaoParaBaseCusto(siglaBase);
    const custoExibicao = fator > 0 ? custo / fator : custo;
    
    if (custoExibicao <= 0 || !siglaExibicao) {
      return '-';
    }

    const valor = this.formatCurrency(custoExibicao);
    return `${valor} / ${siglaExibicao}`;
  }

  // Preview para UI; fonte da verdade = backend
  calcularCustoItem(item: ReceitaItemFormModel): number {
    if (!item.insumoId || item.quantidade <= 0) return 0;
    const insumo = this.insumos().find(i => i.id === item.insumoId);
    if (!insumo) return 0;

    const custoPorUnidadeUso = this.calcularCustoPorUnidadeUso(insumo);
    const unidadeSigla = this.obterSiglaUnidade(item.unidadeMedidaId).toUpperCase();
    if (unidadeSigla === 'UN') {
      if (!insumo.pesoPorUnidade || insumo.pesoPorUnidade <= 0) {
        return 0;
      }
      const pesoPorUnidade = this.ajustarPesoPorUnidade(insumo.pesoPorUnidade, insumo.unidadeCompraSigla);
      return item.quantidade * pesoPorUnidade * custoPorUnidadeUso;
    }
    return item.quantidade * custoPorUnidadeUso;
  }

  // Preview para UI; fonte da verdade = backend
  get custoTotalCalculado(): number | null {
    let custoTotalBruto = 0;
    let temItensValidos = false;
    
    for (const item of this.itens()) {
      if (!item.insumoId || item.quantidade <= 0) continue;
      const custoItem = this.calcularCustoItem(item);
      custoTotalBruto += custoItem;
      temItensValidos = true;
    }
    
    if (!temItensValidos) return null;
    
    // OPÇÃO B (contrato): IC/FatorRendimento não altera custo total — apenas rendimento/peso final.
    return custoTotalBruto;
  }

  // Preview para UI; fonte da verdade = backend
  get custoPorPorcaoCalculado(): number | null {
    const custoTotal = this.custoTotalCalculado;
    if (custoTotal === null) return null;

    const pesoFinal = this.pesoTotalAposRendimento; // agora só massa (GR + UN em g) * IC
    const pesoPorPorcao = this.model.pesoPorPorcao;

    // NOVA REGRA (cliente) só se NÃO houver ML/L (volume sem densidade)
    if (!this.temItensEmMl && pesoFinal !== null && pesoFinal > 0 && pesoPorPorcao && pesoPorPorcao > 0) {
      return custoTotal * (pesoPorPorcao / pesoFinal);
    }

    // fallback legado (se houver ML ou sem peso para calcular)
    const rendimento = this.model.rendimento;
    if (rendimento <= 0) return null;
    return custoTotal / rendimento;
  }

  atualizarCustosItens(): void {
    const currentItens = this.itens();
    let houveMudanca = false;

    currentItens.forEach(item => {
      const custoItem = this.calcularCustoItem(item);
      const novoValor = custoItem > 0 ? custoItem : undefined;
      if (item.custoItem !== novoValor) {
        item.custoItem = novoValor;
        houveMudanca = true;
      }
    });

    if (houveMudanca) {
      this.cdr.markForCheck();
    }
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;
    const file = input.files[0];
    
    // Validar tipo de arquivo
    const valid = ['image/png', 'image/jpeg'].includes(file.type);
    if (!valid) { 
      this.toast.error('Apenas imagens PNG ou JPG'); 
      input.value = ''; 
      return; 
    }
    
    this.upload.uploadAvatar(file)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.model.pathImagem = res.url;
          this.toast.success('Imagem atualizada');
          this.cdr.markForCheck();
        },
        error: () => {
          this.toast.error('Erro ao fazer upload da imagem');
          this.cdr.markForCheck();
        }
      });
  }

  save() {
    this.error.set('');
    if (this.isView()) return;
    
    const v = this.model;
    if (!v.nome || !v.categoriaId || v.rendimento <= 0) {
      this.toast.error('Preencha os campos obrigatórios corretamente');
      return;
    }

    const validItens = this.itens().filter(item => 
      item.insumoId !== null && 
      item.quantidade > 0 && 
      item.unidadeMedidaId !== null
    );
    if (validItens.length === 0) {
      this.toast.error('Adicione pelo menos um item vÃ¡lido Ã  receita (com insumo, quantidade e unidade de medida)');
      return;
    }
    // Validação defensiva: garantir que todas as unidades são base (GR/ML/UN)
    const unidadesInvalidas = validItens.filter(item => {
      const unidade = this.unidades().find(u => u.id === item.unidadeMedidaId);
      if (!unidade) return true;
      const sigla = (unidade.sigla || '').toUpperCase();
      return sigla !== 'GR' && sigla !== 'ML' && sigla !== 'UN';
    });
    
    if (unidadesInvalidas.length > 0) {
      const siglasInvalidas = unidadesInvalidas.map(item => {
        const unidade = this.unidades().find(u => u.id === item.unidadeMedidaId);
        return unidade?.sigla || '?';
      }).join(', ');
      this.toast.error(`Erro: existem itens com unidades inválidas (${siglasInvalidas}). Use apenas GR, ML ou UN.`);
      return;
    }
    // Validações do IC
    if (v.icSinal === '-' && v.icValor !== null && v.icValor >= 100) {
      this.toast.error('Perda de 100% ou mais resultaria em peso final zero. Use um valor menor que 100%.');
      return;
    }

    // Arredondar quantidades baseado na unidade de medida antes de enviar
    const itensRequest = validItens.map(item => {
      let quantidade = item.quantidade;
      
      // Se for unidade UN, garantir que seja inteiro
      if (item.unidadeMedidaId) {
        const unidade = this.unidades().find(u => u.id === item.unidadeMedidaId);
        if (unidade && unidade.sigla.toUpperCase() === 'UN') {
          quantidade = Math.round(quantidade);
          if (quantidade < 1) quantidade = 1;
        }
      }
      
      return {
        insumoId: item.insumoId!,
        quantidade: quantidade,
        unidadeMedidaId: item.unidadeMedidaId!,
        exibirComoQB: item.exibirComoQB,
        ordem: item.ordem,
        observacoes: item.observacoes || undefined
      };
    });

    if (this.id() === null) {
      const req: CreateReceitaRequest = {
        nome: v.nome,
        categoriaId: v.categoriaId!,
        conservacao: v.conservacao || undefined,
        descricao: v.descricao || undefined,
        instrucoesEmpratamento: v.instrucoesEmpratamento || undefined,
        rendimento: v.rendimento,
        pesoPorPorcao: v.pesoPorPorcao || undefined,
        fatorRendimento: 1.0, // Backend calcula a partir de icSinal/icValor, sempre enviar 1.0
        icSinal: v.icSinal || undefined,
        icValor: v.icValor ?? undefined,
        tempoPreparo: v.tempoPreparo || undefined,
        versao: v.versao || '1.0',
        pathImagem: v.pathImagem || undefined,
        itens: itensRequest as CreateReceitaItemRequest[],
        isAtivo: !!v.isAtivo
      };
      this.service.create(req)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => { 
            this.toast.success('Receita criada'); 
            this.router.navigate(['/tenant/receitas']); 
          },
          error: err => { 
            const msg = err.error?.message || 'Erro ao salvar receita'; 
            this.toast.error(msg); 
            this.error.set(msg);
            this.cdr.markForCheck();
          }
        });
    } else {
      const req: UpdateReceitaRequest = {
        nome: v.nome,
        categoriaId: v.categoriaId!,
        conservacao: v.conservacao || undefined,
        descricao: v.descricao || undefined,
        instrucoesEmpratamento: v.instrucoesEmpratamento || undefined,
        rendimento: v.rendimento,
        pesoPorPorcao: v.pesoPorPorcao || undefined,
        fatorRendimento: 1.0, // Backend calcula a partir de icSinal/icValor, sempre enviar 1.0
        icSinal: v.icSinal || undefined,
        icValor: v.icValor ?? undefined,
        tempoPreparo: v.tempoPreparo || undefined,
        versao: v.versao || '1.0',
        pathImagem: v.pathImagem || undefined,
        itens: itensRequest as UpdateReceitaItemRequest[],
        isAtivo: !!v.isAtivo
      };
      this.service.update(this.id()!, req)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => { 
            this.toast.success('Receita atualizada'); 
            this.router.navigate(['/tenant/receitas']); 
          },
          error: err => { 
            const msg = err.error?.message || 'Erro ao salvar receita'; 
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

    this.http.get(`${this.environment.apiUrl}/tenant/receitas/${currentId}/pdf`, {
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
}
