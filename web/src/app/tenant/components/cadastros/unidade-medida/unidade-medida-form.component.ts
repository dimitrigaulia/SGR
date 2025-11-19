import { Component, inject, signal, ChangeDetectionStrategy, ChangeDetectorRef, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { ToastService } from '../../../../core/services/toast.service';
import { UnidadeMedidaService, CreateUnidadeMedidaRequest, UpdateUnidadeMedidaRequest } from '../../../../features/tenant-unidades-medida/services/unidade-medida.service';

@Component({
  standalone: true,
  selector: 'app-tenant-unidade-medida-form',
  imports: [CommonModule, FormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule, MatSelectModule, MatSlideToggleModule, MatSnackBarModule],
  templateUrl: './unidade-medida-form.component.html',
  styleUrls: ['./unidade-medida-form.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TenantUnidadeMedidaFormComponent {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private service = inject(UnidadeMedidaService);
  private toast = inject(ToastService);
  private cdr = inject(ChangeDetectorRef);
  private destroyRef = inject(DestroyRef);

  id = signal<number | null>(null);
  isView = signal<boolean>(false);
  error = signal<string>('');
  model = { nome: '', sigla: '', tipo: '', unidadeBaseId: null as number | null, fatorConversaoBase: null as number | null, isAtivo: true };
  unidadesBase = signal<Array<{ id: number; nome: string; sigla: string }>>([]);
  
  tiposUnidade = [
    { value: 'Peso', label: 'Peso' },
    { value: 'Volume', label: 'Volume' },
    { value: 'Quantidade', label: 'Quantidade' }
  ];

  constructor() {
    const st: any = this.router.getCurrentNavigation()?.extras.state ?? window.history.state;
    const id = st?.id as number | undefined;
    const view = !!st?.view;
    this.isView.set(view);

    // Carregar unidades base para o select (sempre, para criação e edição)
    this.service.list({ pageSize: 1000 })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(result => {
        this.unidadesBase.set(result.items.map(u => ({ id: u.id, nome: u.nome, sigla: u.sigla })));
        this.cdr.markForCheck();
      });

    if (id) {
      this.id.set(id);
      this.service.get(id)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(e => { 
          this.model = { 
            nome: e.nome, 
            sigla: e.sigla, 
            tipo: e.tipo || '', 
            unidadeBaseId: e.unidadeBaseId || null,
            fatorConversaoBase: e.fatorConversaoBase || null,
            isAtivo: e.isAtivo 
          }; 
          this.cdr.markForCheck();
        });
    }
  }

  save() {
    this.error.set('');
    if (this.isView()) return;

    if (!this.model.nome || !this.model.sigla) { 
      this.toast.error('Nome e sigla são obrigatórios'); 
      return; 
    }

    // Validações entre UnidadeBase e FatorConversaoBase para evitar cadastros ambíguos
    if (this.model.unidadeBaseId && (!this.model.fatorConversaoBase || this.model.fatorConversaoBase <= 0)) {
      this.toast.error('Informe o fator de conversão base quando selecionar uma unidade base (ex.: 0,001 para g → kg).');
      return;
    }

    if (!this.model.unidadeBaseId && this.model.fatorConversaoBase != null) {
      this.toast.error('Para usar fator de conversão base, selecione também uma unidade base ou remova o fator.');
      return;
    }

    const v = this.model;

    if (this.id() === null) {
      const req: CreateUnidadeMedidaRequest = { 
        nome: v.nome, 
        sigla: v.sigla, 
        tipo: v.tipo || undefined,
        unidadeBaseId: v.unidadeBaseId || undefined,
        fatorConversaoBase: v.fatorConversaoBase || undefined,
        isAtivo: !!v.isAtivo 
      };
      this.service.create(req)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => { this.toast.success('Unidade de medida criada'); this.router.navigate(['/tenant/unidades-medida']); },
          error: err => { 
            const msg = err.error?.message || 'Erro ao salvar unidade de medida'; 
            this.toast.error(msg); 
            this.error.set(msg);
            this.cdr.markForCheck();
          }
        });
    } else {
      const req: UpdateUnidadeMedidaRequest = { 
        nome: v.nome, 
        sigla: v.sigla, 
        tipo: v.tipo || undefined,
        unidadeBaseId: v.unidadeBaseId || undefined,
        fatorConversaoBase: v.fatorConversaoBase || undefined,
        isAtivo: !!v.isAtivo 
      };
      this.service.update(this.id()!, req)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => { this.toast.success('Unidade de medida atualizada'); this.router.navigate(['/tenant/unidades-medida']); },
          error: err => { 
            const msg = err.error?.message || 'Erro ao salvar unidade de medida'; 
            this.toast.error(msg); 
            this.error.set(msg);
            this.cdr.markForCheck();
          }
        });
    }
  }
}

