import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { ToastService } from '../../../services/toast.service';
import { PerfilService, CreatePerfilRequest, UpdatePerfilRequest } from '../../../services/perfil.service';

@Component({
  standalone: true,
  selector: 'app-perfil-form',
  imports: [CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatSlideToggleModule, MatSnackBarModule],
  templateUrl: './perfil-form.component.html',
  styleUrls: ['./perfil-form.component.scss']
})
export class PerfilFormComponent {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private service = inject(PerfilService);
  private toast = inject(ToastService);

  id = signal<number | null>(null);
  isView = signal<boolean>(false);
  error = signal<string>('');

  form = this.fb.group({
    nome: ['', [Validators.required, Validators.maxLength(100)]],
    isAtivo: [true]
  });

  constructor() {
    const st: any = this.router.getCurrentNavigation()?.extras.state ?? window.history.state;
    const id = st?.id as number | undefined;
    const view = !!st?.view;
    this.isView.set(view);
    if (view) this.form.disable();
    if (id) {
      this.id.set(id);
      this.service.get(id).subscribe(e => {
        this.form.patchValue({ nome: e.nome, isAtivo: e.isAtivo });
      });
    }
  }

  save() {
    this.error.set('');
    if (this.form.invalid) return;
    if (this.isView()) return;
    const v = this.form.value;
    if (this.id() === null) {
      const req: CreatePerfilRequest = { nome: v.nome!, isAtivo: !!v.isAtivo };
      this.service.create(req).subscribe({
        next: () => { this.toast.success('Perfil criado'); this.router.navigate(['/perfis']); },
        error: err => { const msg = err.error?.message || 'Erro ao salvar perfil'; this.toast.error(msg); this.error.set(msg); }
      });
    } else {
      const req: UpdatePerfilRequest = { nome: v.nome!, isAtivo: !!v.isAtivo };
      this.service.update(this.id()!, req).subscribe({
        next: () => { this.toast.success('Perfil atualizado'); this.router.navigate(['/perfis']); },
        error: err => { const msg = err.error?.message || 'Erro ao salvar perfil'; this.toast.error(msg); this.error.set(msg); }
      });
    }
  }
}
