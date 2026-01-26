import { Component, inject, signal, computed, DestroyRef, ChangeDetectorRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDividerModule } from '@angular/material/divider';
import { MatExpansionModule } from '@angular/material/expansion';
import { ReceitaService, ReceitaDto } from '../../../../features/tenant-receitas/services/receita.service';
import { InsumoService, InsumoDto } from '../../../../features/tenant-insumos/services/insumo.service';
import { UnidadeMedidaService, UnidadeMedidaDto } from '../../../../features/tenant-unidades-medida/services/unidade-medida.service';

type ItemEscalado = {
  key: string;
  insumoId: number;
  insumoNome: string;
  qtd: number;
  unidadeSigla: string;
  obs: string;
  checked: boolean;
};

@Component({
  standalone: true,
  selector: 'app-tenant-receita-cozinha',
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatButtonToggleModule,
    MatFormFieldModule,
    MatInputModule,
    MatCardModule,
    MatListModule,
    MatCheckboxModule,
    MatDividerModule,
    MatExpansionModule
  ],
  templateUrl: './receita-cozinha.component.html',
  styleUrls: ['./receita-cozinha.component.scss']
})
export class TenantReceitaCozinhaComponent {
  private router = inject(Router);
  private service = inject(ReceitaService);
  private insumoService = inject(InsumoService);
  private unidadeService = inject(UnidadeMedidaService);
  private cdr = inject(ChangeDetectorRef);
  private destroyRef = inject(DestroyRef);

  id = signal<number | null>(null);
  model = signal<ReceitaDto | null>(null);
  insumos = signal<InsumoDto[]>([]);
  unidades = signal<UnidadeMedidaDto[]>([]);

  // Meta de produção
  metaTipo = signal<'porcoes' | 'peso'>('porcoes');
  metaValor = signal<number>(1);

  // Timer
  timerAtivo = signal<boolean>(false);
  timerLabel = signal<string>('00:00');
  private timerInterval: any = null;
  private timerSegundosRestantes = 0;

  // Itens escalados
  itensEscalados = signal<ItemEscalado[]>([]);

  constructor() {
    // Carregar insumos e unidades
    this.insumoService.list({ pageSize: 1000 })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.insumos.set(res.items.filter(i => i.isAtivo));
          this.cdr.markForCheck();
        }
      });

    this.unidadeService.list({ pageSize: 1000 })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.unidades.set(res.items.filter(u => u.isAtivo));
          this.cdr.markForCheck();
        }
      });

    // Ler ID da receita
    const st: any = this.router.getCurrentNavigation()?.extras.state ?? (typeof window !== 'undefined' ? (window as any).history?.state : undefined);
    const id = st?.id as number | undefined;

    if (id) {
      this.id.set(id);
      this.service.get(id)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(receita => {
          this.model.set(receita);
          this.metaValor.set(receita.rendimento);
          this.recalcularItensEscalados();
          this.cdr.markForCheck();
        });
    } else {
      this.router.navigate(['/tenant/receitas']);
    }
  }

  // Computed: Peso base disponível (soma itens em GR)
  pesoBaseDisponivel = computed(() => {
    const receita = this.model();
    if (!receita) return false;

    let temGR = false;
    for (const item of receita.itens) {
      const unidade = this.unidades().find(u => u.id === item.unidadeMedidaId);
      if (unidade && unidade.sigla.toUpperCase() === 'GR') {
        temGR = true;
        break;
      }
    }
    return temGR;
  });

  // Computed: Peso total dos itens em GR (após IC)
  pesoTotalAposRendimento = computed(() => {
    const receita = this.model();
    if (!receita) return null;

    let total = 0;
    let temPeso = false;

    for (const item of receita.itens) {
      const unidade = this.unidades().find(u => u.id === item.unidadeMedidaId);
      if (!unidade || unidade.sigla.toUpperCase() !== 'GR') continue;

      total += item.quantidade;
      temPeso = true;
    }

    if (!temPeso) return null;

    // Aplicar IC
    const sinal = receita.icSinal || '-';
    const valor = receita.icValor ?? 0;
    const delta = Math.max(0, Math.min(999, valor)) / 100;
    const fatorRendimento = delta === 0 ? 1.0 : (sinal === '-' ? 1 - delta : 1 + delta);

    return total * fatorRendimento;
  });

  // Computed: Fator de escala
  fatorEscala = computed(() => {
    const receita = this.model();
    const tipo = this.metaTipo();
    const valor = this.metaValor();
    if (!receita || valor <= 0) return 1;

    if (tipo === 'porcoes') {
      return valor / receita.rendimento;
    } else {
      const pesoBase = this.pesoTotalAposRendimento();
      if (pesoBase === null || pesoBase <= 0) return 1;
      return valor / pesoBase;
    }
  });

  // Computed: Porções de operação
  porcoesOperacao = computed(() => {
    const receita = this.model();
    const tipo = this.metaTipo();
    const valor = this.metaValor();
    if (!receita) return 0;

    if (tipo === 'porcoes') {
      return valor;
    } else {
      return receita.rendimento * this.fatorEscala();
    }
  });

  // Computed: Peso final de operação
  pesoFinalOperacao = computed(() => {
    const pesoBase = this.pesoTotalAposRendimento();
    if (pesoBase === null) return null;
    return pesoBase * this.fatorEscala();
  });

  // Computed: Peso por porção de operação
  pesoPorPorcaoOperacao = computed(() => {
    const pesoFinal = this.pesoFinalOperacao();
    const porcoes = this.porcoesOperacao();
    if (pesoFinal === null || porcoes <= 0) return null;
    return pesoFinal / porcoes;
  });

  // Itens escalados por tipo
  itensEscaladosPeso = computed(() => {
    return this.itensEscalados().filter(it => it.unidadeSigla === 'GR');
  });

  itensEscaladosVolume = computed(() => {
    return this.itensEscalados().filter(it => it.unidadeSigla === 'ML');
  });

  itensEscaladosUnidade = computed(() => {
    return this.itensEscalados().filter(it => it.unidadeSigla === 'UN');
  });

  onMetaTipoChange(tipo: 'porcoes' | 'peso') {
    this.metaTipo.set(tipo);
    
    // Ajustar metaValor baseado no tipo
    const receita = this.model();
    if (!receita) return;

    if (tipo === 'porcoes') {
      this.metaValor.set(receita.rendimento);
    } else {
      const pesoBase = this.pesoTotalAposRendimento();
      if (pesoBase) {
        this.metaValor.set(Math.round(pesoBase));
      }
    }

    this.recalcularItensEscalados();
    this.cdr.markForCheck();
  }

  onMetaValorChange(valor: number) {
    if (valor < 1) valor = 1;
    this.metaValor.set(valor);
    this.recalcularItensEscalados();
    this.cdr.markForCheck();
  }

  private recalcularItensEscalados() {
    const receita = this.model();
    if (!receita) return;

    const fator = this.fatorEscala();
    const itens: ItemEscalado[] = [];

    for (const item of receita.itens) {
      const insumo = this.insumos().find(i => i.id === item.insumoId);
      const unidade = this.unidades().find(u => u.id === item.unidadeMedidaId);
      if (!insumo || !unidade) continue;

      const sigla = unidade.sigla.toUpperCase();
      let qtdEscalada = item.quantidade * fator;

      // UN: arredondar para cima
      if (sigla === 'UN') {
        qtdEscalada = Math.ceil(qtdEscalada);
      } else {
        // GR/ML: arredondar para inteiro
        qtdEscalada = Math.round(qtdEscalada);
      }

      itens.push({
        key: `${item.insumoId}-${item.unidadeMedidaId}`,
        insumoId: item.insumoId,
        insumoNome: insumo.nome,
        qtd: qtdEscalada,
        unidadeSigla: sigla,
        obs: item.observacoes || '',
        checked: false
      });
    }

    this.itensEscalados.set(itens);
  }

  marcarTodos(checked: boolean) {
    const itens = this.itensEscalados();
    itens.forEach(it => it.checked = checked);
    this.itensEscalados.set([...itens]);
    this.cdr.markForCheck();
  }

  startTimer(minutos: number) {
    if (this.timerAtivo()) {
      this.stopTimer();
    }

    this.timerSegundosRestantes = minutos * 60;
    this.timerAtivo.set(true);
    this.atualizarTimerLabel();

    this.timerInterval = setInterval(() => {
      this.timerSegundosRestantes--;
      if (this.timerSegundosRestantes <= 0) {
        this.stopTimer();
        // Opcional: tocar som ou notificação
        alert('Timer finalizado!');
      } else {
        this.atualizarTimerLabel();
      }
      this.cdr.markForCheck();
    }, 1000);
  }

  stopTimer() {
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
      this.timerInterval = null;
    }
    this.timerAtivo.set(false);
    this.timerLabel.set('00:00');
    this.cdr.markForCheck();
  }

  private atualizarTimerLabel() {
    const minutos = Math.floor(this.timerSegundosRestantes / 60);
    const segundos = this.timerSegundosRestantes % 60;
    this.timerLabel.set(`${minutos.toString().padStart(2, '0')}:${segundos.toString().padStart(2, '0')}`);
  }

  toggleFullscreen() {
    if (!document.fullscreenElement) {
      document.documentElement.requestFullscreen().catch(err => {
        console.error('Erro ao entrar em tela cheia:', err);
      });
    } else {
      document.exitFullscreen();
    }
  }

  printPdf() {
    const id = this.id();
    if (!id) return;
    window.open(`/api/tenant/receitas/${id}/pdf`, '_blank');
  }
}
