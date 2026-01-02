import { Component, inject, signal, DestroyRef, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { TenantService, TenantDto } from '../../../features/tenants/services/tenant.service';
import { TenantRefreshService } from '../../../core/services/tenant-refresh.service';

/**
 * Componente para seleção e gerenciamento de impersonação de tenants pelo backoffice
 */
@Component({
  selector: 'app-tenant-selector',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatSelectModule,
    MatFormFieldModule,
    MatIconModule,
    MatChipsModule,
    MatTooltipModule
  ],
  templateUrl: './tenant-selector.component.html',
  styleUrl: './tenant-selector.component.scss'
})
export class TenantSelectorComponent implements OnInit {
  private authService = inject(AuthService);
  private tenantService = inject(TenantService);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  private tenantRefreshService = inject(TenantRefreshService);

  tenants = signal<TenantDto[]>([]);
  selectedTenant = signal<string>('');
  isImpersonating = signal(false);
  isLoading = signal(false);
  impersonatedTenantName = signal<string>('');

  ngOnInit(): void {
    // Verificar se já está impersonando
    const impersonatedTenant = this.authService.getImpersonatedTenant();
    if (impersonatedTenant) {
      this.isImpersonating.set(true);
      this.selectedTenant.set(impersonatedTenant);
      this.loadTenantName(impersonatedTenant);
    }

    // Carregar lista de tenants ativos
    this.loadTenants();

    // Se inscrever em eventos de refresh
    this.tenantRefreshService.refresh$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.refreshTenants();
      });
  }

  loadTenants(): void {
    this.isLoading.set(true);
    this.tenantService.getActiveTenants()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (tenants) => {
          this.tenants.set(tenants);
          this.isLoading.set(false);
        },
        error: () => {
          this.isLoading.set(false);
        }
      });
  }

  /**
   * Método público para recarregar a lista de tenants
   * Pode ser chamado externamente após operações CRUD
   */
  public refreshTenants(): void {
    this.loadTenants();
    
    // Se estiver impersonando, atualizar também o nome do tenant
    const impersonatedTenant = this.authService.getImpersonatedTenant();
    if (impersonatedTenant) {
      this.loadTenantName(impersonatedTenant);
    }
  }

  loadTenantName(subdomain: string): void {
    this.tenantService.getBySubdomain(subdomain)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (tenant) => {
          this.impersonatedTenantName.set(tenant.nomeFantasia);
        },
        error: () => {
          this.impersonatedTenantName.set(subdomain);
        }
      });
  }

  startImpersonation(): void {
    const subdomain = this.selectedTenant();
    if (!subdomain) {
      return;
    }

    this.authService.setImpersonatedTenant(subdomain);
    this.isImpersonating.set(true);
    this.loadTenantName(subdomain);
    
    // Redirecionar para o dashboard do tenant
    this.router.navigate(['/tenant/dashboard']);
  }

  stopImpersonation(): void {
    this.authService.clearImpersonation();
    this.isImpersonating.set(false);
    this.selectedTenant.set('');
    this.impersonatedTenantName.set('');
    
    // Redirecionar para o dashboard do backoffice
    this.router.navigate(['/backoffice/dashboard']);
  }
}
