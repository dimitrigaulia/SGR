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
  model = { nome: '', sigla: '', isAtivo: true };

  constructor() {
    const st: any = this.router.getCurrentNavigation()?.extras.state ?? window.history.state;
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
            sigla: e.sigla, 
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
      this.toast.error('Nome e sigla sÃ£o obrigatÃ³rios'); 
      return; 
    }

    const v = this.model;

    if (this.id() === null) {
      const req: CreateUnidadeMedidaRequest = { 
        nome: v.nome, 
        sigla: v.sigla, 
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

