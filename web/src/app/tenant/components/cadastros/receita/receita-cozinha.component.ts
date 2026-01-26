import { Component, inject, signal, computed, effect, DestroyRef, ChangeDetectorRef, AfterViewInit } from '@angular/core';
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
export class TenantReceitaCozinhaComponent implements AfterViewInit {
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
  alarmAtivo = signal<boolean>(false);

  // Meta de produção
  metaTipo = signal<'porcoes' | 'peso'>('porcoes');
  metaValor = signal<number>(1);

  // Timer
  timerAtivo = signal<boolean>(false);
  timerLabel = signal<string>('00:00');
  private timerInterval: any = null;
  private timerSegundosRestantes = 0;
  private titleInterval: any = null;
  private originalTitle: string = (typeof document !== 'undefined' && document.title) ? document.title : '';
  private audioInterval: any = null;
  private alarmTimeout: any = null;

  // Itens escalados
  itensEscalados = signal<ItemEscalado[]>([]);

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

  // Computed: Peso total dos itens em g/ml (após IC)
  pesoTotalAposRendimento = computed(() => {
    const receita = this.model();
    if (!receita) return null;

    // Somar peso de todos os itens usando o campo pesoItemGml calculado no backend
    let total = 0;
    let temPeso = false;

    for (const item of receita.itens) {
      if (item.pesoItemGml && item.pesoItemGml > 0) {
        total += item.pesoItemGml;
        temPeso = true;
      }
    }

    if (!temPeso) return null;

    // Aplicar IC (Índice de Cocção)
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

  // Computed: Recalcula itens escalados automaticamente quando dependências mudam
  private itensEscaladosComputed = computed(() => {
    const receita = this.model();
    const insumos = this.insumos();
    const unidades = this.unidades();
    const fator = this.fatorEscala();

    if (!receita || !receita.itens.length) return [];
    if (!insumos.length || !unidades.length) return []; // Aguardar carregamento

    const itens: ItemEscalado[] = [];

    for (const item of receita.itens) {
      const insumo = insumos.find(i => i.id === item.insumoId);
      const unidade = unidades.find(u => u.id === item.unidadeMedidaId);
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

    return itens;
  });

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
          this.cdr.markForCheck();
        });
    } else {
      this.router.navigate(['/tenant/receitas']);
    }

    // Sincronizar itensEscalados com computed sempre que mudar (reactivo)
    effect(() => {
      const itens = this.itensEscaladosComputed();
      this.itensEscalados.set(itens);
      this.cdr.markForCheck();
    }, { allowSignalWrites: true });

    this.destroyRef.onDestroy(() => {
      if (this.timerInterval) clearInterval(this.timerInterval);
      this.stopAlarm();
    });
  }

  ngAfterViewInit() {
    // Inicializar itens escalados após a view estar pronta
    setTimeout(() => {
      const itens = this.itensEscaladosComputed();
      this.itensEscalados.set(itens);
      this.cdr.markForCheck();
    }, 0);
  }

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

    // Recalcular usando computed
    this.itensEscalados.set(this.itensEscaladosComputed());
    this.cdr.markForCheck();
  }

  onMetaValorChange(valor: number) {
    if (valor < 1) valor = 1;
    this.metaValor.set(valor);
    // Recalcular usando computed
    this.itensEscalados.set(this.itensEscaladosComputed());
    this.cdr.markForCheck();
  }

  private recalcularItensEscalados() {
    // Mantido para compatibilidade, mas agora usa computed
    this.itensEscalados.set(this.itensEscaladosComputed());
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
        // Tocar som e notificação visual
        this.triggerAlarm('Timer finalizado!');
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
    // parar qualquer flash de título
    if (this.titleInterval) {
      clearInterval(this.titleInterval);
      this.titleInterval = null;
      if (typeof document !== 'undefined') document.title = this.originalTitle;
    }
    this.cdr.markForCheck();
  }

  private triggerAlarm(message: string, options?: { loopUntilStopped?: boolean; durationMs?: number }) {
    const loop = !!options?.loopUntilStopped;
    const duration = options?.durationMs ?? (loop ? null : 5000);

    // mark alarm active
    this.alarmAtivo.set(true);

    // start audio loop if loop requested, otherwise play once and optionally repeat until duration
    if (loop) {
      // play immediately then keep interval
      this.playBeep();
      if (this.audioInterval) clearInterval(this.audioInterval);
      this.audioInterval = setInterval(() => this.playBeep(), 1200);
    } else {
      // play immediately; if duration > single sequence length, schedule repeats
      this.playBeep();
      if (duration && duration > 1200) {
        const repeats = Math.floor(duration / 1200);
        let i = 0;
        if (this.audioInterval) clearInterval(this.audioInterval);
        this.audioInterval = setInterval(() => {
          i++;
          if (i > repeats) {
            if (this.audioInterval) { clearInterval(this.audioInterval); this.audioInterval = null; }
          } else {
            this.playBeep();
          }
        }, 1200);
        // safety timeout
        if (this.alarmTimeout) clearTimeout(this.alarmTimeout);
        this.alarmTimeout = setTimeout(() => this.stopAlarm(), duration + 200);
      }
    }

    // visual alert: pass duration (null means until stopAlarm called)
    this.showVisualAlert(message, duration ?? null);
  }

  private playBeep() {
    try {
      const AudioCtx = (window as any).AudioContext || (window as any).webkitAudioContext;
      if (!AudioCtx) return;
      const ctx = new AudioCtx();

      const playTone = (freq: number, dur = 200, delay = 0) => {
        const now = ctx.currentTime + delay / 1000;
        const o = ctx.createOscillator();
        const g = ctx.createGain();
        o.type = 'sawtooth';
        o.frequency.setValueAtTime(freq, now);
        g.gain.setValueAtTime(0.0001, now);
        g.gain.linearRampToValueAtTime(0.25, now + 0.01);
        g.gain.linearRampToValueAtTime(0.0001, now + dur / 1000);
        o.connect(g);
        g.connect(ctx.destination);
        o.start(now);
        o.stop(now + dur / 1000 + 0.02);
      };

      // Sequência de pulsos: três pulsos descendentes para maior percepção
      playTone(1500, 180, 0);
      playTone(1200, 180, 220);
      playTone(900, 300, 460);

      // Garantir fechamento do contexto após toda a sequência
      setTimeout(() => {
        try { ctx.close(); } catch {}
      }, 1200);

      // Tentar vibrar se disponível
      try { if (navigator && 'vibrate' in navigator) (navigator as any).vibrate?.([200, 100, 300]); } catch {}
    } catch (e) {
      // silencioso em caso de erro
    }
  }

  // Método público para testes no template (5s)
  testAlarm() {
    this.triggerAlarm('Teste de alarme', { durationMs: 5000 });
  }

  // Método público para teste em loop até o usuário parar
  testAlarmLoop() {
    this.triggerAlarm('Teste de alarme (loop até parar)', { loopUntilStopped: true });
  }

  private async showVisualAlert(message: string, durationMs: number | null = 5000) {
    // Tentar Notification API
    try {
      if ('Notification' in window) {
        if (Notification.permission === 'granted') {
          new Notification(message);
        } else if (Notification.permission !== 'denied') {
          const perm = await Notification.requestPermission();
          if (perm === 'granted') new Notification(message);
        }
      }
    } catch {}

    // Flashar o título da página como fallback/visível
    if (typeof document !== 'undefined') {
      const flashText = message + ' — ' + (this.originalTitle || '');
      let visible = true;
      // Limpar qualquer intervalo pré-existente
      if (this.titleInterval) {
        clearInterval(this.titleInterval);
        this.titleInterval = null;
      }
      let count = 0;
      const maxCount = durationMs === null ? Infinity : Math.ceil((durationMs || 0) / 500);
      this.titleInterval = setInterval(() => {
        try {
          document.title = visible ? flashText : this.originalTitle;
        } catch {}
        visible = !visible;
        count++;
        if (count >= maxCount) {
          if (this.titleInterval) { clearInterval(this.titleInterval); this.titleInterval = null; }
          try { document.title = this.originalTitle; } catch {}
        }
      }, 500);
    }
  }

  // Para parar alarmes em looping ou limpar timeouts
  stopAlarm() {
    if (this.audioInterval) {
      try { clearInterval(this.audioInterval); } catch {}
      this.audioInterval = null;
    }
    if (this.alarmTimeout) {
      try { clearTimeout(this.alarmTimeout); } catch {}
      this.alarmTimeout = null;
    }
    if (this.titleInterval) {
      try { clearInterval(this.titleInterval); } catch {}
      this.titleInterval = null;
      try { if (typeof document !== 'undefined') document.title = this.originalTitle; } catch {}
    }
    this.alarmAtivo.set(false);
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
