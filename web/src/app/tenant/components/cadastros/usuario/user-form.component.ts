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
import { TenantUsuarioService, CreateTenantUsuarioRequest, UpdateTenantUsuarioRequest, TenantUsuarioDto } from '../../../../features/tenant-usuarios/services/usuario.service';
import { TenantPerfilService, TenantPerfilDto } from '../../../../features/tenant-perfis/services/perfil.service';
import { ToastService } from '../../../../core/services/toast.service';
import { UploadService } from '../../../../features/usuarios/services/upload.service';

type TenantUsuarioFormModel = Omit<TenantUsuarioDto, 'id'> & {
  senha?: string;
  novaSenha?: string;
};

@Component({
  standalone: true,
  selector: 'app-tenant-user-form',
  imports: [CommonModule, FormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule, MatSelectModule, MatSlideToggleModule, MatSnackBarModule],
  templateUrl: './user-form.component.html',
  styleUrls: ['./user-form.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TenantUserFormComponent {
  private router = inject(Router);
  private service = inject(TenantUsuarioService);
  private perfilService = inject(TenantPerfilService);
  private toast = inject(ToastService);
  private upload = inject(UploadService);
  private cdr = inject(ChangeDetectorRef);
  private destroyRef = inject(DestroyRef);

  id = signal<number | null>(null);
  perfis = signal<TenantPerfilDto[]>([]);
  isEdit = computed(() => this.id() !== null);
  isView = signal<boolean>(false);
  error = signal<string>('');
  previousAvatarUrl: string | null = null;
  emailTaken = false;
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

  model: TenantUsuarioFormModel = {
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
    // ValidaÃ§Ã£o simples
    if (!v.nomeCompleto || !v.email || !v.perfilId || this.emailTaken) {
      this.toast.error('Preencha os campos obrigatÃ³rios corretamente');
      return;
    }

    if (!this.isEdit()) {
      const req: CreateTenantUsuarioRequest = {
        nomeCompleto: v.nomeCompleto,
        email: v.email,
        perfilId: v.perfilId!,
        isAtivo: !!v.isAtivo,
        senha: v.senha || '',
        pathImagem: v.pathImagem || undefined,
      };
      this.service.create(req)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => { this.toast.success('UsuÃ¡rio criado'); this.router.navigate(['/tenant/usuarios']); },
          error: (err) => { 
            const msg = err.error?.message || 'Erro ao salvar usuÃ¡rio'; 
            this.toast.error(msg); 
            this.error.set(msg);
            this.cdr.markForCheck();
          }
        });
    } else {
      const req: UpdateTenantUsuarioRequest = {
        nomeCompleto: v.nomeCompleto,
        email: v.email,
        perfilId: v.perfilId!,
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
            this.toast.success('UsuÃ¡rio atualizado');
            this.router.navigate(['/tenant/usuarios']);
          },
          error: (err) => { 
            const msg = err.error?.message || 'Erro ao salvar usuÃ¡rio'; 
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
}

