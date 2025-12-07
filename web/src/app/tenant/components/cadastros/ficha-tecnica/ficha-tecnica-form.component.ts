import { Component, ChangeDetectionStrategy, ChangeDetectorRef, DestroyRef, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
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
import { ToastService } from '../../../../core/services/toast.service';
import { environment } from '../../../../../environments/environment';
import { CategoriaReceitaService, CategoriaReceitaDto } from '../../../../features/tenant-categorias-receita/services/categoria-receita.service';
import { ReceitaService, ReceitaDto } from '../../../../features/tenant-receitas/services/receita.service';
import { InsumoService, InsumoDto } from '../../../../features/tenant-insumos/services/insumo.service';
import { UnidadeMedidaService, UnidadeMedidaDto } from '../../../../features/tenant-unidades-medida/services/unidade-medida.service';
import { FichaTecnicaService, CreateFichaTecnicaRequest, UpdateFichaTecnicaRequest, FichaTecnicaItemDto } from '../../../../features/tenant-receitas/services/ficha-tecnica.service';

type FichaTecnicaCanalFormModel = {
  id?: number | null;
  canal: string;
  nomeExibicao: string;
  precoVenda: number;
  taxaPercentual?: number | null;
  comissaoPercentual?: number | null;
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
  imports: [CommonModule, FormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule, MatSelectModule, MatSlideToggleModule, MatSnackBarModule, MatTableModule, MatIconModule, MatCardModule, MatCheckboxModule, MatTooltipModule],
  templateUrl: './ficha-tecnica-form.component.html',
  styleUrls: ['./ficha-tecnica-form.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TenantFichaTecnicaFormComponent {
  private router = inject(Router);
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

  model = {
    categoriaId: null as number | null,
    nome: '',
    codigo: '',
    descricaoComercial: '',
    indiceContabil: null as number | null,
    icOperador: null as string | null,
    icValor: null as number | null,
    ipcValor: null as number | null,
    margemAlvoPercentual: null as number | null,
    isAtivo: true
  };

  itens = signal<FichaTecnicaItemFormModel[]>([]);
  canais = signal<FichaTecnicaCanalFormModel[]>([]);
  displayedColumnsItens = ['ordem', 'tipo', 'item', 'quantidade', 'unidade', 'qb', 'observacoes', 'acoes'];
  displayedColumns = ['canal', 'nomeExibicao', 'precoVenda', 'taxas', 'margem', 'acoes'];
  
  // Propriedades para uso no template
  window = typeof window !== 'undefined' ? window : null;
  environment = environment;

  custoTotal = signal<number>(0);
  custoPorUnidade = signal<number>(0);
  rendimentoFinal = signal<number | null>(null);
  precoSugeridoVenda = signal<number | null>(null);

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

    if (id) {
      this.id.set(id);
      this.fichaService.get(id)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(e => {
          this.model = {
            categoriaId: e.categoriaId,
            nome: e.nome,
            codigo: e.codigo || '',
            descricaoComercial: e.descricaoComercial || '',
            indiceContabil: e.indiceContabil ?? null,
            icOperador: e.icOperador ?? null,
            icValor: e.icValor ?? null,
            ipcValor: e.ipcValor ?? null,
            margemAlvoPercentual: e.margemAlvoPercentual ?? null,
            isAtivo: e.isAtivo
          };
          this.custoTotal.set(e.custoTotal);
          this.custoPorUnidade.set(e.custoPorUnidade);
          this.rendimentoFinal.set(e.rendimentoFinal ?? null);
          this.precoSugeridoVenda.set(e.precoSugeridoVenda ?? null);
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
            margemCalculadaPercentual: c.margemCalculadaPercentual ?? null,
            observacoes: c.observacoes || '',
            isAtivo: c.isAtivo
          })));
          this.cdr.markForCheck();
        });
    } else {
      this.addCanal();
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
    this.itens.set(current.filter((_, i) => i !== index));
    this.cdr.markForCheck();
  }

  onItemTipoChange(item: FichaTecnicaItemFormModel) {
    if (item.tipoItem === 'Receita') {
      item.insumoId = null;
    } else {
      item.receitaId = null;
    }
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

  addCanal() {
    const current = this.canais();
    this.canais.set([...current, {
      canal: '',
      nomeExibicao: '',
      precoVenda: 0,
      taxaPercentual: null,
      comissaoPercentual: null,
      margemCalculadaPercentual: null,
      observacoes: '',
      isAtivo: true
    }]);
    this.cdr.markForCheck();
  }

  addCanalPreset(tipo: 'IFOOD1' | 'IFOOD2' | 'BALCAO' | 'DELIVERY') {
    const presets: Record<string, { canal: string; nomeExibicao: string; taxaPercentual?: number | null; comissaoPercentual?: number | null }> = {
      IFOOD1: { canal: 'ifood-1', nomeExibicao: 'Ifood 1', taxaPercentual: 13, comissaoPercentual: null },
      IFOOD2: { canal: 'ifood-2', nomeExibicao: 'Ifood 2', taxaPercentual: 25, comissaoPercentual: null },
      BALCAO: { canal: 'BALCAO', nomeExibicao: 'BalcÃ£o', taxaPercentual: 0, comissaoPercentual: null },
      DELIVERY: { canal: 'DELIVERY', nomeExibicao: 'Delivery PrÃ³prio', taxaPercentual: 0, comissaoPercentual: null }
    };

    const preset = presets[tipo];
    const current = this.canais();
    this.canais.set([...current, {
      canal: preset.canal,
      nomeExibicao: preset.nomeExibicao,
      precoVenda: 0,
      taxaPercentual: preset.taxaPercentual ?? null,
      comissaoPercentual: preset.comissaoPercentual ?? null,
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

    if (this.id() === null) {
      const req: CreateFichaTecnicaRequest = {
        categoriaId: v.categoriaId,
        nome: v.nome,
        codigo: v.codigo || undefined,
        descricaoComercial: v.descricaoComercial || undefined,
        indiceContabil: v.indiceContabil ?? undefined,
        icOperador: v.icOperador || undefined,
        icValor: v.icValor ?? undefined,
        ipcValor: v.ipcValor ?? undefined,
        margemAlvoPercentual: v.margemAlvoPercentual ?? undefined,
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
          observacoes: c.observacoes || undefined,
          isAtivo: !!c.isAtivo
        }))
      };

      this.fichaService.create(req)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.toast.success('Ficha tÃ©cnica criada');
            this.router.navigate(['/tenant/fichas-tecnicas']);
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
        nome: v.nome,
        codigo: v.codigo || undefined,
        descricaoComercial: v.descricaoComercial || undefined,
        indiceContabil: v.indiceContabil ?? undefined,
        icOperador: v.icOperador || undefined,
        icValor: v.icValor ?? undefined,
        ipcValor: v.ipcValor ?? undefined,
        margemAlvoPercentual: v.margemAlvoPercentual ?? undefined,
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
          observacoes: c.observacoes || undefined,
          isAtivo: !!c.isAtivo
        }))
      };

      this.fichaService.update(this.id()!, req)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.toast.success('Ficha tÃ©cnica atualizada');
            this.router.navigate(['/tenant/fichas-tecnicas']);
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
}
