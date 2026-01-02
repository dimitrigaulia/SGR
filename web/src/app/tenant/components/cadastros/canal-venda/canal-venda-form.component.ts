import { Component, inject, signal, ChangeDetectionStrategy, ChangeDetectorRef, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { ToastService } from '../../../../core/services/toast.service';
import { CanalVendaService, CreateCanalVendaRequest, UpdateCanalVendaRequest } from '../../../../features/tenant-canais-venda/services/canal-venda.service';

@Component({
  standalone: true,
  selector: 'app-tenant-canal-venda-form',
  imports: [CommonModule, FormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule, MatSlideToggleModule, MatSnackBarModule],
  templateUrl: './canal-venda-form.component.html',
  styleUrls: ['./canal-venda-form.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TenantCanalVendaFormComponent {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private service = inject(CanalVendaService);
  private toast = inject(ToastService);
  private cdr = inject(ChangeDetectorRef);
  private destroyRef = inject(DestroyRef);

  id = signal<number | null>(null);
  isView = signal<boolean>(false);
  error = signal<string>('');
  model = {
    nome: '',
    taxaPercentualPadrao: null as number | null,
    isAtivo: true
  };

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
            taxaPercentualPadrao: e.taxaPercentualPadrao ?? null,
            isAtivo: e.isAtivo
          }; 
          this.cdr.markForCheck();
        });
    }
  }

  save() {
    this.error.set('');
    if (this.isView()) return;
    if (!this.model.nome) { this.toast.error('Nome é obrigatório'); return; }
    
    const v = this.model;
    if (this.id() === null) {
      const req: CreateCanalVendaRequest = {
        nome: v.nome,
        taxaPercentualPadrao: v.taxaPercentualPadrao,
        isAtivo: !!v.isAtivo
      };
      this.service.create(req)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => { this.toast.success('Canal de venda criado'); this.router.navigate(['/tenant/canais-venda']); },
          error: err => { 
            const msg = err.error?.message || 'Erro ao salvar canal de venda'; 
            this.toast.error(msg); 
            this.error.set(msg);
            this.cdr.markForCheck();
          }
        });
    } else {
      const req: UpdateCanalVendaRequest = {
        nome: v.nome,
        taxaPercentualPadrao: v.taxaPercentualPadrao,
        isAtivo: !!v.isAtivo
      };
      this.service.update(this.id()!, req)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => { this.toast.success('Canal de venda atualizado'); this.router.navigate(['/tenant/canais-venda']); },
          error: err => { 
            const msg = err.error?.message || 'Erro ao salvar canal de venda'; 
            this.toast.error(msg); 
            this.error.set(msg);
            this.cdr.markForCheck();
          }
        });
    }
  }
}
