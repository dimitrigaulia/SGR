import { Component, computed, inject, signal, ViewChild, ElementRef, ChangeDetectionStrategy, ChangeDetectorRef, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { UsuarioService, CreateUsuarioRequest, UpdateUsuarioRequest, UsuarioDto } from '../../../../features/usuarios/services/usuario.service';
import { PerfilService, PerfilDto } from '../../../../features/perfis/services/perfil.service';
import { ToastService } from '../../../../core/services/toast.service';
import { UploadService } from '../../../../features/usuarios/services/upload.service';

// Tipo do formulário - perfilId pode ser null apenas durante a inicialização/seleção
// Na validação garantimos que será number antes de salvar (perfil é obrigatório)
type UsuarioFormModel = Omit<UsuarioDto, 'id' | 'perfilId'> & {
  perfilId: number | null; // null apenas no formulário, obrigatório ao salvar
  senha?: string;
  novaSenha?: string;
};

@Component({
  standalone: true,
  selector: 'app-user-form',
  imports: [CommonModule, FormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule, MatSelectModule, MatSlideToggleModule, MatSnackBarModule],
  templateUrl: './user-form.component.html',
  styleUrls: ['./user-form.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserFormComponent {
  private router = inject(Router);
  private service = inject(UsuarioService);
  private perfilService = inject(PerfilService);
  private toast = inject(ToastService);
  private upload = inject(UploadService);
  private cdr = inject(ChangeDetectorRef);
  private destroyRef = inject(DestroyRef);

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
    perfilId: null,
    isAtivo: true,
    senha: '',
    novaSenha: '',
    pathImagem: ''
  };

  constructor() {
    // Carregar perfis (lista simples)
    this.perfilService.list({ pageSize: 1000 })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ 
        next: res => {
          this.perfis.set(res.items);
          this.cdr.markForCheck();
        }
      });

    // Ler state (id/view)
    const st: any = this.router.getCurrentNavigation()?.extras.state ?? (typeof window !== 'undefined' ? (window as any).history?.state : undefined);
    const id = st?.id as number | undefined;
    const view = !!st?.view;
    this.isView.set(view);
    if (id) {
      this.id.set(id);
      this.service.get(id)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe(e => {
          this.model = { ...e, senha: '', novaSenha: '' };
          this.previousAvatarUrl = e.pathImagem ?? null;
          this.cdr.markForCheck();
        });
    }
  }

  save() {
    this.error.set('');
    if (this.isView()) return;
    const v = this.model;
    
    // Validações específicas com mensagens apropriadas
    if (this.emailTaken) {
      this.toast.error('Este email já está em uso');
      return;
    }
    
    if (!v.nomeCompleto?.trim()) {
      this.toast.error('Nome completo é obrigatório');
      return;
    }
    
    if (!v.email?.trim()) {
      this.toast.error('Email é obrigatório');
      return;
    }
    
    if (v.perfilId == null || v.perfilId === undefined) {
      this.toast.error('Selecione um perfil');
      return;
    }
    
    // Validação adicional para senha ao criar
    if (!this.isEdit() && (!v.senha || v.senha.trim().length < 6)) {
      this.toast.error('A senha deve ter pelo menos 6 caracteres');
      return;
    }

    // Neste ponto, perfilId é garantidamente number (não null) devido à validação acima
    const perfilId = v.perfilId;

    if (!this.isEdit()) {
      const req: CreateUsuarioRequest = {
        nomeCompleto: v.nomeCompleto.trim(),
        email: v.email.trim(),
        perfilId: perfilId, // Agora TypeScript sabe que é number
        isAtivo: !!v.isAtivo,
        senha: v.senha || '',
        pathImagem: v.pathImagem || undefined,
      };
      this.service.create(req)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => { this.toast.success('Usuário criado'); this.router.navigate(['/backoffice/usuarios']); },
          error: (err) => { 
            const msg = err.error?.message || 'Erro ao salvar usuário'; 
            this.toast.error(msg); 
            this.error.set(msg);
            this.cdr.markForCheck();
          }
        });
    } else {
      const req: UpdateUsuarioRequest = {
        nomeCompleto: v.nomeCompleto.trim(),
        email: v.email.trim(),
        perfilId: perfilId, // Usa a variável já validada
        isAtivo: !!v.isAtivo,
        novaSenha: v.novaSenha || undefined,
        pathImagem: v.pathImagem || undefined,
      };
      this.service.update(this.id()!, req)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            const newUrl = req.pathImagem || '';
            if (this.previousAvatarUrl && this.previousAvatarUrl !== newUrl && this.previousAvatarUrl.includes('/avatars/')) {
              this.upload.deleteAvatar(this.previousAvatarUrl)
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe({ next: () => {}, error: () => {} });
            }
            this.previousAvatarUrl = newUrl || null;
            this.toast.success('Usuário atualizado');
            this.router.navigate(['/backoffice/usuarios']);
          },
          error: (err) => { 
            const msg = err.error?.message || 'Erro ao salvar usuário'; 
            this.toast.error(msg); 
            this.error.set(msg);
            this.cdr.markForCheck();
          }
        });
    }
  }

  triggerFile() { this.fileInput?.nativeElement.click(); }

  clearAvatar() {
    const current = this.model.pathImagem || '';
    if (current && current.includes('/avatars/')) {
      this.upload.deleteAvatar(current)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({ next: () => {}, error: () => {} });
    }
    this.model.pathImagem = '';
    if (this.fileInput) this.fileInput.nativeElement.value = '';
    this.cdr.markForCheck();
  }

  onFile(evt: Event) {
    const input = evt.target as HTMLInputElement;
    const file = input.files && input.files[0];
    if (!file) return;
    const valid = ['image/png', 'image/jpeg'].includes(file.type);
    if (!valid) { this.toast.error('Apenas imagens PNG ou JPG'); input.value=''; return; }
    this.upload.uploadAvatar(file)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.model.pathImagem = res.url;
          this.toast.success('Foto atualizada');
          this.cdr.markForCheck();
        },
        error: () => { 
          this.toast.error('Falha ao enviar imagem');
          this.cdr.markForCheck();
        }
      });
  }

  onEmailBlur(value: string) {
    if (!value || !value.includes('@')) { 
      this.emailTaken = false; 
      this.cdr.markForCheck();
      return; 
    }
    const excludeId = this.id() ?? undefined;
    this.service.checkEmail(value, excludeId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ 
        next: res => {
          this.emailTaken = res.exists;
          this.cdr.markForCheck();
        }, 
        error: () => {
          this.emailTaken = false;
          this.cdr.markForCheck();
        }
      });
  }

  onPerfilChange() {
    this.cdr.markForCheck();
  }
}

