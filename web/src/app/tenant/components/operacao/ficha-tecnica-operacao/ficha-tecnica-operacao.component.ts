import { ChangeDetectionStrategy, ChangeDetectorRef, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatTabsModule } from '@angular/material/tabs';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ToastService } from '../../../../core/services/toast.service';
import { LoadingComponent } from '../../../../shared/components/loading/loading.component';
import { FichaTecnicaDto, FichaTecnicaItemDto, FichaTecnicaService } from '../../../../features/tenant-receitas/services/ficha-tecnica.service';
import { ReceitaDto, ReceitaItemDto, ReceitaService } from '../../../../features/tenant-receitas/services/receita.service';

@Component({
  standalone: true,
  selector: 'app-tenant-ficha-tecnica-operacao',
  imports: [
    CommonModule,
    RouterLink,
    MatTabsModule,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    LoadingComponent
  ],
  templateUrl: './ficha-tecnica-operacao.component.html',
  styleUrls: ['./ficha-tecnica-operacao.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TenantFichaTecnicaOperacaoComponent {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fichaService = inject(FichaTecnicaService);
  private receitaService = inject(ReceitaService);
  private toast = inject(ToastService);
  private cdr = inject(ChangeDetectorRef);
  private destroyRef = inject(DestroyRef);

  id = signal<number | null>(null);
  ficha = signal<FichaTecnicaDto | null>(null);
  receita = signal<ReceitaDto | null>(null);
  isLoading = signal(false);
  error = signal<string>('');

  selectedTab = signal(0); // 0=comercial, 1=producao

  displayedColumnsItens = ['ordem', 'insumo', 'quantidadeTotal', 'quantidadePorPorcao', 'custoTotal', 'custoPorPorcao'];
  displayedColumnsCanais = ['canal', 'precoVenda', 'taxas', 'margem'];

  titulo = computed(() => {
    const f = this.ficha();
    const r = this.receita();
    return f?.nome || r?.nome || 'Operação';
  });

  constructor() {
    const idRaw = this.route.snapshot.paramMap.get('id');
    const id = idRaw ? Number(idRaw) : NaN;
    if (!idRaw || Number.isNaN(id) || id <= 0) {
      this.error.set('ID inválido');
      return;
    }

    this.id.set(id);

    const tab = (this.route.snapshot.queryParamMap.get('tab') || '').toLowerCase();
    this.selectedTab.set(tab === 'producao' || tab === 'produção' ? 1 : 0);

    this.load(id);
  }

  onTabChange(index: number) {
    this.selectedTab.set(index);
    const tab = index === 1 ? 'producao' : 'comercial';
    this.router.navigate([], { relativeTo: this.route, queryParams: { tab }, queryParamsHandling: 'merge' });
  }

  private load(id: number) {
    this.isLoading.set(true);
    this.error.set('');

    this.fichaService.get(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (ficha) => {
          this.ficha.set(ficha);
          const receitaId = ficha.receitaPrincipalId ?? null;
          if (!receitaId) {
            this.receita.set(null);
            this.isLoading.set(false);
            this.cdr.markForCheck();
            return;
          }

          this.receitaService.get(receitaId)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe({
              next: (receita) => {
                this.receita.set(receita);
                this.isLoading.set(false);
                this.cdr.markForCheck();
              },
              error: () => {
                this.toast.error('Falha ao carregar receita principal');
                this.receita.set(null);
                this.isLoading.set(false);
                this.cdr.markForCheck();
              }
            });
        },
        error: () => {
          this.toast.error('Falha ao carregar ficha técnica');
          this.error.set('Falha ao carregar ficha técnica');
          this.isLoading.set(false);
          this.cdr.markForCheck();
        }
      });
  }

  get itensMiseEnPlace(): ReceitaItemDto[] {
    return (this.receita()?.itens ?? []).slice().sort((a, b) => (a.ordem ?? 0) - (b.ordem ?? 0));
  }

  quantidadePorPorcao(item: ReceitaItemDto): number | null {
    const r = this.receita();
    const rendimento = r?.rendimento ?? 0;
    if (!rendimento || rendimento <= 0) return null;
    if (item.exibirComoQB) return null;
    return item.quantidade / rendimento;
  }

  custoPorPorcao(item: ReceitaItemDto): number | null {
    const r = this.receita();
    const rendimento = r?.rendimento ?? 0;
    if (!rendimento || rendimento <= 0) return null;
    return (item.custoItem ?? 0) / rendimento;
  }

  formatCurrency(value: number | null | undefined): string {
    return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value || 0);
  }

  formatTime(minutes: number | null | undefined): string {
    if (!minutes) return '-';
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    if (hours > 0) return `${hours}h ${mins}min`;
    return `${mins}min`;
  }

  quantidadeDisplay(item: ReceitaItemDto): string {
    if (item.exibirComoQB) return 'QB';
    const unidade = item.unidadeMedidaSigla || '';
    return `${(item.quantidade ?? 0).toFixed(4)} ${unidade}`.trim();
  }

  // Métodos para trabalhar com itens da ficha técnica (quando não há receita principal)
  get itensFichaTecnica(): FichaTecnicaItemDto[] {
    return (this.ficha()?.itens ?? []).slice().sort((a, b) => (a.ordem ?? 0) - (b.ordem ?? 0));
  }

  quantidadePorPorcaoFicha(item: FichaTecnicaItemDto): number | null {
    const f = this.ficha();
    if (item.exibirComoQB) return null;
    
    // Para receitas na ficha técnica, a quantidade já é em porções
    if (item.tipoItem === 'Receita') {
      return item.quantidade;
    }
    
    // Para insumos, calcular baseado na porção de venda ou rendimento final
    if (f?.porcaoVendaQuantidade && f.porcaoVendaQuantidade > 0 && f.rendimentoFinal && f.rendimentoFinal > 0) {
      // Se há porção de venda definida, calcular proporcionalmente
      const proporcao = f.porcaoVendaQuantidade / f.rendimentoFinal;
      return item.quantidade * proporcao;
    } else if (f?.rendimentoFinal && f.rendimentoFinal > 0) {
      // Se não há porção definida, usar rendimento final como base (1 porção = rendimento final)
      return item.quantidade / f.rendimentoFinal;
    }
    
    return null;
  }

  custoPorPorcaoFicha(item: FichaTecnicaItemDto): number | null {
    const f = this.ficha();
    
    // Se há porção de venda definida, calcular proporcionalmente
    if (f?.porcaoVendaQuantidade && f.porcaoVendaQuantidade > 0 && f.rendimentoFinal && f.rendimentoFinal > 0) {
      const proporcao = f.porcaoVendaQuantidade / f.rendimentoFinal;
      return (item.custoItem ?? 0) * proporcao;
    } else if (f?.rendimentoFinal && f.rendimentoFinal > 0) {
      // Se não há porção definida, usar rendimento final como base (1 porção = rendimento final)
      return (item.custoItem ?? 0) / f.rendimentoFinal;
    }
    
    return null;
  }

  quantidadeDisplayFicha(item: FichaTecnicaItemDto): string {
    if (item.exibirComoQB) return 'QB';
    if (item.tipoItem === 'Receita') {
      return `${item.quantidade}x`;
    }
    const unidade = item.unidadeMedidaSigla || '';
    return `${(item.quantidade ?? 0).toFixed(4)} ${unidade}`.trim();
  }

  getItemNomeFicha(item: FichaTecnicaItemDto): string {
    return item.insumoNome || item.receitaNome || '-';
  }
}

