import { Component, computed, inject, signal, ViewChild, ElementRef } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormBuilder, ReactiveFormsModule, Validators, AsyncValidatorFn, AbstractControl, ValidationErrors } from "@angular/forms";
import { Router } from "@angular/router";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatButtonModule } from "@angular/material/button";
import { MatSelectModule } from "@angular/material/select";
import { MatSlideToggleModule } from "@angular/material/slide-toggle";
import { MatSnackBarModule } from "@angular/material/snack-bar";
import { of, timer } from "rxjs";
import { switchMap, map, catchError } from "rxjs/operators";
import { UsuarioService, CreateUsuarioRequest, UpdateUsuarioRequest } from "../../../services/usuario.service";
import { PerfilService, PerfilDto } from "../../../services/perfil.service";
import { ToastService } from "../../../services/toast.service";
import { UploadService } from "../../../services/upload.service";

@Component({
  standalone: true,
  selector: 'app-user-form',
  imports: [CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatSelectModule, MatSlideToggleModule, MatSnackBarModule],
  templateUrl: './user-form.component.html',
  styleUrls: ['./user-form.component.scss']
})
export class UserFormComponent {
  private fb = inject(FormBuilder);
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
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

  form = this.fb.group({
    nomeCompleto: ['', [Validators.required, Validators.maxLength(200)]],
    email: this.fb.control('', { validators: [Validators.required, Validators.email, Validators.maxLength(200)], asyncValidators: [this.emailExistsValidator()], updateOn: 'blur' }),
    perfilId: [null as number | null, [Validators.required]],
    isAtivo: [true],
    senha: [''], // create only
    novaSenha: [''], // update only
    pathImagem: ['']
  });

  constructor() {
    // perfis
    this.perfilService.list({ pageSize: 1000 }).subscribe({ next: res => this.perfis.set(res.items) });

    // state (id/view)
    const st: any = this.router.getCurrentNavigation()?.extras.state ?? (typeof window !== 'undefined' ? (window as any).history?.state : undefined);
    const id = st?.id as number | undefined;
    const view = !!st?.view;
    this.isView.set(view);
    if (view) this.form.disable();
    if (id) {
      this.id.set(id);
      this.service.get(id).subscribe(e => {
        this.form.patchValue({
          nomeCompleto: e.nomeCompleto,
          email: e.email,
          perfilId: e.perfilId,
          isAtivo: e.isAtivo,
          pathImagem: e.pathImagem ?? ''
        });
        this.previousAvatarUrl = e.pathImagem ?? null;
      });
    }
  }

  save() {
    this.error.set('');
    if (this.form.invalid) return;
    if (this.isView()) return;

    const v = this.form.value;
    if (!this.isEdit()) {
      const req: CreateUsuarioRequest = {
        nomeCompleto: v.nomeCompleto!,
        email: v.email!,
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
        nomeCompleto: v.nomeCompleto!,
        email: v.email!,
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
    const current = this.form.get('pathImagem')?.value || '';
    if (current && current.includes('/avatars/')) {
      this.upload.deleteAvatar(current).subscribe({ next: () => {}, error: () => {} });
    }
    this.form.patchValue({ pathImagem: '' });
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
        this.form.patchValue({ pathImagem: res.url });
        this.toast.success('Foto atualizada');
      },
      error: () => { this.toast.error('Falha ao enviar imagem'); }
    });
  }

  private emailExistsValidator(): AsyncValidatorFn {
    return (control: AbstractControl) => {
      const value = control.value as string;
      if (!value || !value.includes('@')) return of(null);
      const excludeId = this.id() ?? undefined;
      return timer(300).pipe(
        switchMap(() => this.service.checkEmail(value, excludeId)),
        map(res => (res.exists ? { emailTaken: true } as ValidationErrors : null)),
        catchError(() => of(null))
      );
    };
  }
}
