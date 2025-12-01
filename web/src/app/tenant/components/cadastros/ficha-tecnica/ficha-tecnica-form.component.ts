import { Component, ChangeDetectionStrategy, ChangeDetectorRef, DestroyRef, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { ToastService } from '../../../../core/services/toast.service';
import { ReceitaService, ReceitaDto } from '../../../../features/tenant-receitas/services/receita.service';
import { FichaTecnicaService, CreateFichaTecnicaRequest, UpdateFichaTecnicaRequest } from '../../../../features/tenant-receitas/services/ficha-tecnica.service';

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

@Component({
  standalone: true,
  selector: 'app-tenant-ficha-tecnica-form',
  imports: [CommonModule, FormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule, MatSelectModule, MatSlideToggleModule, MatSnackBarModule, MatTableModule, MatIconModule, MatCardModule],
  templateUrl: './ficha-tecnica-form.component.html',
  styleUrls: ['./ficha-tecnica-form.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TenantFichaTecnicaFormComponent {
  private router = inject(Router);
  private receitaService = inject(ReceitaService);
  private fichaService = inject(FichaTecnicaService);
  private toast = inject(ToastService);
  private cdr = inject(ChangeDetectorRef);
  private destroyRef = inject(DestroyRef);

  id = signal<number | null>(null);
  receitas = signal<ReceitaDto[]>([]);
  isEdit = computed(() => this.id() !== null);
  isView = signal<boolean>(false);
  error = signal<string>('');

  model = {
    receitaId: null as number | null,
    nome: '',
    codigo: '',
    descricaoComercial: '',
    rendimentoFinal: null as number | null,
    indiceContabil: null as number | null,
    margemAlvoPercentual: null as number | null,
    isAtivo: true
  };

  canais = signal<FichaTecnicaCanalFormModel[]>([]);
  displayedColumns = ['canal', 'nomeExibicao', 'precoVenda', 'taxas', 'margem', 'acoes'];

  custoTecnicoTotal = signal<number>(0);
  custoTecnicoPorPorcao = signal<number>(0);
  precoSugeridoVenda = signal<number | null>(null);

  constructor() {
    this.receitaService.list({ pageSize: 1000 })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.receitas.set(res.items);
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
            receitaId: e.receitaId,
            nome: e.nome,
            codigo: e.codigo || '',
            descricaoComercial: e.descricaoComercial || '',
            rendimentoFinal: e.rendimentoFinal ?? null,
            indiceContabil: e.indiceContabil ?? null,
            margemAlvoPercentual: e.margemAlvoPercentual ?? null,
            isAtivo: e.isAtivo
          };
          this.custoTecnicoTotal.set(e.custoTecnicoTotal);
          this.custoTecnicoPorPorcao.set(e.custoTecnicoPorPorcao);
          this.precoSugeridoVenda.set(e.precoSugeridoVenda ?? null);
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

  get receitaSelecionada(): ReceitaDto | undefined {
    const id = this.model.receitaId;
    if (!id) return undefined;
    return this.receitas().find(r => r.id === id);
  }

  onReceitaChange() {
    const r = this.receitaSelecionada;
    if (r) {
      this.custoTecnicoTotal.set(r.custoTotal);
      this.custoTecnicoPorPorcao.set(r.custoPorPorcao);
    } else {
      this.custoTecnicoTotal.set(0);
      this.custoTecnicoPorPorcao.set(0);
    }
    this.cdr.markForCheck();
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value || 0);
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
      IFOOD1: { canal: 'IFOOD1', nomeExibicao: 'Ifood Loja 1', taxaPercentual: 18, comissaoPercentual: null },
      IFOOD2: { canal: 'IFOOD2', nomeExibicao: 'Ifood Loja 2', taxaPercentual: 25, comissaoPercentual: null },
      BALCAO: { canal: 'BALCAO', nomeExibicao: 'Balcão', taxaPercentual: 0, comissaoPercentual: null },
      DELIVERY: { canal: 'DELIVERY', nomeExibicao: 'Delivery Próprio', taxaPercentual: 0, comissaoPercentual: null }
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
    if (!v.receitaId || !v.nome) {
      this.toast.error('Selecione uma receita e informe o nome');
      return;
    }

    const canaisValidos = this.canais().filter(c => c.canal && c.precoVenda > 0);
    if (canaisValidos.length === 0) {
      this.toast.error('Adicione pelo menos um canal com preço de venda');
      return;
    }

    if (this.id() === null) {
      const req: CreateFichaTecnicaRequest = {
        receitaId: v.receitaId,
        nome: v.nome,
        codigo: v.codigo || undefined,
        descricaoComercial: v.descricaoComercial || undefined,
        rendimentoFinal: v.rendimentoFinal ?? undefined,
        indiceContabil: v.indiceContabil ?? undefined,
        margemAlvoPercentual: v.margemAlvoPercentual ?? undefined,
        isAtivo: !!v.isAtivo,
        canais: canaisValidos.map(c => ({
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
            this.toast.success('Ficha técnica criada');
            this.router.navigate(['/tenant/fichas-tecnicas']);
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
        receitaId: v.receitaId,
        nome: v.nome,
        codigo: v.codigo || undefined,
        descricaoComercial: v.descricaoComercial || undefined,
        rendimentoFinal: v.rendimentoFinal ?? undefined,
        indiceContabil: v.indiceContabil ?? undefined,
        margemAlvoPercentual: v.margemAlvoPercentual ?? undefined,
        isAtivo: !!v.isAtivo,
        canais: canaisValidos.map(c => ({
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
            this.toast.success('Ficha técnica atualizada');
            this.router.navigate(['/tenant/fichas-tecnicas']);
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
}
