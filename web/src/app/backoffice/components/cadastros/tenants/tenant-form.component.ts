import { Component, computed, inject, signal, ChangeDetectionStrategy, ChangeDetectorRef, DestroyRef, NgZone } from '@angular/core';
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
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { debounceTime, distinctUntilChanged, switchMap, catchError, of } from 'rxjs';
import { TenantService, CreateTenantRequest, UpdateTenantRequest, TenantDto, CreateAdminRequest, CategoriaTenantDto, CnpjDataResponse } from '../../../../features/tenants/services/tenant.service';
import { CategoriaTenantService } from '../../../../features/tenants/services/categoria-tenant.service';
import { ToastService } from '../../../../core/services/toast.service';
import { TenantRefreshService } from '../../../../core/services/tenant-refresh.service';
import { applyCpfCnpjMask, removeMask } from '../../../../core/utils/mask.utils';
import { generateSubdomain } from '../../../../core/utils/subdomain.utils';

type TenantFormModel = {
  razaoSocial: string;
  nomeFantasia: string;
  tipoPessoaId: number | null;
  cpfCnpj: string;
  subdominio: string;
  categoriaId: number | null;
  isAtivo: boolean;
  admin: {
    nomeCompleto: string;
    email: string;
    senha: string;
    confirmarSenha: string;
  };
};

@Component({
  standalone: true,
  selector: 'app-tenant-form',
  imports: [
    CommonModule, 
    FormsModule, 
    RouterLink, 
    MatFormFieldModule, 
    MatInputModule, 
    MatButtonModule, 
    MatSelectModule, 
    MatSlideToggleModule, 
    MatSnackBarModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './tenant-form.component.html',
  styleUrls: ['./tenant-form.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TenantFormComponent {
  private router = inject(Router);
  private service = inject(TenantService);
  private categoriaService = inject(CategoriaTenantService);
  private toast = inject(ToastService);
  private tenantRefreshService = inject(TenantRefreshService);
  private cdr = inject(ChangeDetectorRef);
  private destroyRef = inject(DestroyRef);
  private ngZone = inject(NgZone);

  id = signal<number | null>(null);
  isEdit = computed(() => this.id() !== null);
  isView = signal<boolean>(false);
  error = signal<string>('');
  showPassword = signal(false);
  showConfirmPassword = signal(false);
  isLoadingCnpj = signal(false);
  categorias = signal<CategoriaTenantDto[]>([]);

  tipoPessoaOptions = [
    { id: 1, nome: 'Pessoa Física' },
    { id: 2, nome: 'Pessoa Jurídica' }
  ];

  model: TenantFormModel = {
    razaoSocial: '',
    nomeFantasia: '',
    tipoPessoaId: null,
    cpfCnpj: '',
    subdominio: '',
    categoriaId: null,
    isAtivo: true,
    admin: {
      nomeCompleto: '',
      email: '',
      senha: '',
      confirmarSenha: ''
    }
  };

  constructor() {
    this.loadCategorias();
    
    // Ler state (id/view)
    const st: any = this.router.getCurrentNavigation()?.extras.state ?? (typeof window !== 'undefined' ? (window as any).history?.state : undefined);
    const id = st?.id as number | undefined;
    const view = !!st?.view;
    this.isView.set(view);
    
    if (id) {
      this.id.set(id);
      this.service.getById(id)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (e) => {
            this.model = {
              razaoSocial: e.razaoSocial,
              nomeFantasia: e.nomeFantasia,
              tipoPessoaId: e.tipoPessoaId,
              cpfCnpj: e.cpfCnpj,
              subdominio: e.subdominio,
              categoriaId: e.categoriaId,
              isAtivo: e.isAtivo,
              admin: {
                nomeCompleto: '',
                email: '',
                senha: '',
                confirmarSenha: ''
              }
            };
            this.cdr.markForCheck();
          },
          error: (err) => {
            this.toast.error(err.error?.message || 'Erro ao carregar tenant');
            this.router.navigate(['/backoffice/tenants']);
          }
        });
    }
  }

  loadCategorias(): void {
    this.categoriaService.getActive()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (cats) => {
          this.categorias.set(cats);
          // Pré-selecionar "Alimentos" por padrão apenas em modo de criação
          if (!this.isEdit() && !this.model.categoriaId) {
            const alimentos = cats.find(c => 
              c.nome.toLowerCase() === 'alimentos' || 
              c.nome.toLowerCase().includes('alimento')
            );
            if (alimentos) {
              this.model.categoriaId = alimentos.id;
              this.cdr.markForCheck();
            }
          }
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.toast.error('Erro ao carregar categorias');
        }
      });
  }

  onTipoPessoaChange(): void {
    // Limpar CPF/CNPJ quando mudar o tipo
    this.model.cpfCnpj = '';
    this.cdr.markForCheck();
  }

  onCpfCnpjInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const value = input.value;
    
    // Aplicar mÃ¡scara
    const masked = applyCpfCnpjMask(value, this.model.tipoPessoaId);
    this.model.cpfCnpj = masked;
    
    // Se for CNPJ (Pessoa Jurídica) e tiver 18 caracteres (máscara completa), buscar dados
    if (this.model.tipoPessoaId === 2 && masked.length === 18) {
      this.buscarDadosCnpj(masked);
    }
    
    this.cdr.markForCheck();
  }

  buscarDadosCnpj(cnpj: string): void {
    const cnpjLimpo = removeMask(cnpj);
    if (cnpjLimpo.length !== 14) return;

    this.isLoadingCnpj.set(true);
    this.service.getCnpjData(cnpjLimpo)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        catchError((err) => {
          this.isLoadingCnpj.set(false);
          this.toast.error('Erro ao buscar dados do CNPJ');
          this.cdr.markForCheck();
          return of(null);
        })
      )
      .subscribe({
        next: (dados) => {
          this.isLoadingCnpj.set(false);
          if (dados) {
            // Atualizar propriedades diretamente dentro do NgZone
            // Isso garante que as mudanÃ§as sejam detectadas pelo Angular
            this.ngZone.run(() => {
              // Verificar ambos os formatos (snake_case e camelCase) para compatibilidade
              // O backend pode retornar em snake_case mesmo com configuração camelCase
              const razaoSocial = (dados as any).razaoSocial || (dados as any).razao_social;
              const nomeFantasia = (dados as any).nomeFantasia || (dados as any).nome_fantasia;
              
              // Atualizar propriedades diretamente
              if (razaoSocial) {
                this.model.razaoSocial = razaoSocial;
              }
              
              // Se não houver nome fantasia, usar a razão social
              if (nomeFantasia) {
                this.model.nomeFantasia = nomeFantasia;
              } else if (razaoSocial) {
                this.model.nomeFantasia = razaoSocial;
              }
              
              // Gerar subdomínio automaticamente se não estiver editando
              if (!this.isEdit() && !this.model.subdominio) {
                const nomeParaSubdominio = nomeFantasia || razaoSocial;
                if (nomeParaSubdominio) {
                  this.model.subdominio = generateSubdomain(nomeParaSubdominio);
                }
              }
              
              // ForÃ§ar detecÃ§Ã£o de mudanÃ§as mÃºltiplas vezes para garantir
              this.cdr.markForCheck();
              this.cdr.detectChanges();
              
              // Usar setTimeout para garantir que o ngModel seja atualizado
              setTimeout(() => {
                this.cdr.detectChanges();
                this.toast.success('Dados do CNPJ carregados com sucesso');
              }, 0);
            });
          } else {
            this.cdr.markForCheck();
          }
        }
      });
  }

  onNomeFantasiaChange(): void {
    // Gerar subdomínio automaticamente apenas na criação
    if (!this.isEdit() && this.model.nomeFantasia && !this.model.subdominio) {
      this.model.subdominio = generateSubdomain(this.model.nomeFantasia);
      this.cdr.markForCheck();
    }
  }

  onSubdominioInput(): void {
    // Normalizar subdomÃ­nio (apenas letras minÃºsculas e nÃºmeros)
    this.model.subdominio = this.model.subdominio.toLowerCase().replace(/[^a-z0-9]/g, '');
    this.cdr.markForCheck();
  }

  save() {
    this.error.set('');
    if (this.isView()) return;

    // Validações
    if (!this.model.razaoSocial || !this.model.nomeFantasia || !this.model.tipoPessoaId || 
        !this.model.cpfCnpj || !this.model.subdominio || !this.model.categoriaId) {
      this.toast.error('Preencha todos os campos obrigatórios');
      return;
    }

    if (!this.isEdit() && (!this.model.admin.nomeCompleto || !this.model.admin.email || 
        !this.model.admin.senha || !this.model.admin.confirmarSenha)) {
      this.toast.error('Preencha todos os dados do administrador');
      return;
    }

    if (!this.isEdit() && this.model.admin.senha !== this.model.admin.confirmarSenha) {
      this.toast.error('As senhas não coincidem');
      return;
    }

    if (!this.isEdit() && this.model.admin.senha.length < 6) {
      this.toast.error('A senha deve ter no mínimo 6 caracteres');
      return;
    }

    if (this.isEdit()) {
      const req: UpdateTenantRequest = {
        razaoSocial: this.model.razaoSocial,
        nomeFantasia: this.model.nomeFantasia,
        tipoPessoaId: this.model.tipoPessoaId!,
        cpfCnpj: removeMask(this.model.cpfCnpj), // Remove mÃ¡scara antes de enviar
        categoriaId: this.model.categoriaId!,
        isAtivo: this.model.isAtivo
      };
      
      this.service.update(this.id()!, req)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.toast.success('Tenant atualizado com sucesso');
            // Notificar TenantSelector para atualizar a lista
            this.tenantRefreshService.notifyRefresh();
            this.router.navigate(['/backoffice/tenants']);
          },
          error: (err) => {
            const msg = err.error?.message || 'Erro ao atualizar tenant';
            this.toast.error(msg);
            this.error.set(msg);
            this.cdr.markForCheck();
          }
        });
    } else {
      const req: CreateTenantRequest = {
        razaoSocial: this.model.razaoSocial,
        nomeFantasia: this.model.nomeFantasia,
        tipoPessoaId: this.model.tipoPessoaId!,
        cpfCnpj: removeMask(this.model.cpfCnpj), // Remove mÃ¡scara antes de enviar
        subdominio: this.model.subdominio,
        categoriaId: this.model.categoriaId!,
        admin: {
          nomeCompleto: this.model.admin.nomeCompleto,
          email: this.model.admin.email,
          senha: this.model.admin.senha,
          confirmarSenha: this.model.admin.confirmarSenha
        }
      };
      
      this.service.create(req)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            this.toast.success('Tenant criado com sucesso');
            // Notificar TenantSelector para atualizar a lista
            this.tenantRefreshService.notifyRefresh();
            this.router.navigate(['/backoffice/tenants']);
          },
          error: (err) => {
            const msg = err.error?.message || 'Erro ao criar tenant';
            this.toast.error(msg);
            this.error.set(msg);
            this.cdr.markForCheck();
          }
        });
    }
  }

  togglePasswordVisibility() {
    this.showPassword.set(!this.showPassword());
  }

  toggleConfirmPasswordVisibility() {
    this.showConfirmPassword.set(!this.showConfirmPassword());
  }
}
