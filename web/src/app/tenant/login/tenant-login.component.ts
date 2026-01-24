import { Component, inject, DestroyRef, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';
import { TenantService, TenantDto } from '../../features/tenants/services/tenant.service';

/**
 * Componente de login do tenant (com seleÃ§Ã£o de tenant)
 */
@Component({
  selector: 'app-tenant-login',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule
  ],
  templateUrl: './tenant-login.component.html',
  styleUrl: './tenant-login.component.scss',
})
export class TenantLoginComponent {
  private authService = inject(AuthService);
  private tenantService = inject(TenantService);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  model = {
    tenantSubdomain: '',
    email: '',
    senha: ''
  };
  
  tenants = signal<TenantDto[]>([]);
  isLoading = signal(false);
  isLoadingTenants = signal(false);
  errorMessage = signal('');

  constructor() {
    this.loadTenants();
  }

  loadTenants(): void {
    this.isLoadingTenants.set(true);
    this.tenantService.getActiveTenants()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (tenants) => {
          this.tenants.set(tenants);
          this.isLoadingTenants.set(false);
        },
        error: (error) => {
          this.errorMessage.set('Erro ao carregar tenants. Tente novamente.');
          this.isLoadingTenants.set(false);
        }
      });
  }

  onSubmit(form: any): void {
    if (form.invalid || !this.model.tenantSubdomain) {
      if (!this.model.tenantSubdomain) {
        this.errorMessage.set('Selecione um tenant');
      }
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    this.authService.loginTenant(
      { email: this.model.email, senha: this.model.senha },
      this.model.tenantSubdomain
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isLoading.set(false);
          this.router.navigate(['/tenant/dashboard']);
        },
        error: (error) => {
          this.isLoading.set(false);
          this.errorMessage.set(error.error?.message || 'Email ou senha inválidos.');
        }
      });
  }
}

