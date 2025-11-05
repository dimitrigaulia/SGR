import { Component, computed, inject, signal, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { UsuarioService, CreateUsuarioRequest, UpdateUsuarioRequest, UsuarioDto } from '../../../services/usuario.service';
import { PerfilService, PerfilDto } from '../../../services/perfil.service';
import { ToastService } from '../../../services/toast.service';
import { UploadService } from '../../../services/upload.service';

type UsuarioFormModel = Omit<UsuarioDto, 'id'> & {
  senha?: string;
  novaSenha?: string;
};

@Component({
  standalone: true,
  selector: 'app-user-form',
  imports: [CommonModule, FormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule, MatSelectModule, MatSlideToggleModule, MatSnackBarModule],
  templateUrl: './user-form.component.html',
  styleUrls: ['./user-form.component.scss']
})
export class UserFormComponent {
  private router = inject(Router);
  private service = inject(UsuarioService);
  private perfilService = inject(PerfilService);
  private toast = inject(ToastService);
  private upload = inject(UploadService);

  id = signal<number | null>(null);
  perfis = signal<PerfilDto[]>([]);
  isEdit = computed(() => this.id() !== null);
  isView = signal<boolean>(false);
  error = signal<string>('');
  previousAvatarUrl: string | null = null;
  emailTaken = false;
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

  model: UsuarioFormModel = {
    nomeCompleto: '',
    email: '',
    perfilId: null as any,
    isAtivo: true,
    senha: '',
    novaSenha: '',
    pathImagem: ''
  };

  constructor() {
    // Carregar perfis (lista simples)
    this.perfilService.list({ pageSize: 1000 }).subscribe({ next: res => this.perfis.set(res.items) });

    // Ler state (id/view)
    const st: any = this.router.getCurrentNavigation()?.extras.state ?? (typeof window !== 'undefined' ? (window as any).history?.state : undefined);
    const id = st?.id as number | undefined;
    const view = !!st?.view;
    this.isView.set(view);
    if (id) {
      this.id.set(id);
      this.service.get(id).subscribe(e => {
        this.model = { ...e, senha: '', novaSenha: '' };
        this.previousAvatarUrl = e.pathImagem ?? null;
      });
    }
  }

  save() {
    this.error.set('');
    if (this.isView()) return;
    const v = this.model;
    // Validação simples
    if (!v.nomeCompleto || !v.email || !v.perfilId || this.emailTaken) {
      this.toast.error('Preencha os campos obrigatórios corretamente');
      return;
    }

    if (!this.isEdit()) {
      const req: CreateUsuarioRequest = {
        nomeCompleto: v.nomeCompleto,
        email: v.email,
        perfilId: v.perfilId!,
        isAtivo: !!v.isAtivo,
        senha: v.senha || '',
        pathImagem: v.pathImagem || undefined,
      };
      this.service.create(req).subscribe({
        next: () => { this.toast.success('Usuário criado'); this.router.navigate(['/usuarios']); },
        error: (err) => { const msg = err.error?.message || 'Erro ao salvar usuário'; this.toast.error(msg); this.error.set(msg); }
      });
    } else {
      const req: UpdateUsuarioRequest = {
        nomeCompleto: v.nomeCompleto,
        email: v.email,
        perfilId: v.perfilId!,
        isAtivo: !!v.isAtivo,
        novaSenha: v.novaSenha || undefined,
        pathImagem: v.pathImagem || undefined,
      };
      this.service.update(this.id()!, req).subscribe({
        next: () => {
          const newUrl = req.pathImagem || '';
          if (this.previousAvatarUrl && this.previousAvatarUrl !== newUrl && this.previousAvatarUrl.includes('/avatars/')) {
            this.upload.deleteAvatar(this.previousAvatarUrl).subscribe({ next: () => {}, error: () => {} });
          }
          this.previousAvatarUrl = newUrl || null;
          this.toast.success('Usuário atualizado');
          this.router.navigate(['/usuarios']);
        },
        error: (err) => { const msg = err.error?.message || 'Erro ao salvar usuário'; this.toast.error(msg); this.error.set(msg); }
      });
    }
  }

  triggerFile() { this.fileInput?.nativeElement.click(); }

  clearAvatar() {
    const current = this.model.pathImagem || '';
    if (current && current.includes('/avatars/')) {
      this.upload.deleteAvatar(current).subscribe({ next: () => {}, error: () => {} });
    }
    this.model.pathImagem = '';
    if (this.fileInput) this.fileInput.nativeElement.value = '';
  }

  onFile(evt: Event) {
    const input = evt.target as HTMLInputElement;
    const file = input.files && input.files[0];
    if (!file) return;
    const valid = ['image/png', 'image/jpeg'].includes(file.type);
    if (!valid) { this.toast.error('Apenas imagens PNG ou JPG'); input.value=''; return; }
    this.upload.uploadAvatar(file).subscribe({
      next: (res) => {
        this.model.pathImagem = res.url;
        this.toast.success('Foto atualizada');
      },
      error: () => { this.toast.error('Falha ao enviar imagem'); }
    });
  }

  onEmailBlur(value: string) {
    if (!value || !value.includes('@')) { this.emailTaken = false; return; }
    const excludeId = this.id() ?? undefined;
    this.service.checkEmail(value, excludeId).subscribe({ next: res => this.emailTaken = res.exists, error: () => this.emailTaken = false });
  }
}

