import { Component, inject, signal, ViewChild, ChangeDetectionStrategy, DestroyRef, ChangeDetectorRef } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { Router, RouterLink } from "@angular/router";
import { BreakpointObserver, Breakpoints } from "@angular/cdk/layout";
import { MatTableModule } from "@angular/material/table";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatTooltipModule } from "@angular/material/tooltip";
import { MatSnackBarModule } from "@angular/material/snack-bar";
import { MatPaginator, MatPaginatorModule, PageEvent } from "@angular/material/paginator";
import { MatSort, MatSortModule, Sort } from "@angular/material/sort";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatCardModule } from "@angular/material/card";
import { MatChipsModule } from "@angular/material/chips";
import { MatDialogModule } from "@angular/material/dialog";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { ToastService } from "../../../../core/services/toast.service";
import { ConfirmationService } from "../../../../core/services/confirmation.service";
import { TenantService, TenantDto } from "../../../../features/tenants/services/tenant.service";
import { LoadingComponent } from "../../../../shared/components/loading/loading.component";

@Component({
  standalone: true,
  selector: 'app-tenants-list',
  imports: [
    CommonModule, 
    FormsModule, 
    RouterLink, 
    MatTableModule, 
    MatButtonModule, 
    MatIconModule, 
    MatTooltipModule, 
    MatSnackBarModule, 
    MatPaginatorModule, 
    MatSortModule, 
    MatFormFieldModule, 
    MatInputModule, 
    MatCardModule,
    MatChipsModule,
    MatDialogModule,
    LoadingComponent
  ],
  templateUrl: './tenants-list.component.html',
  styleUrls: ['./tenants-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TenantsListComponent {
  private service = inject(TenantService);
  private router = inject(Router);
  private toast = inject(ToastService);
  private confirmationService = inject(ConfirmationService);
  private breakpointObserver = inject(BreakpointObserver);
  private destroyRef = inject(DestroyRef);
  private cdr = inject(ChangeDetectorRef);
  
  displayedColumns = ['nomeFantasia', 'razaoSocial', 'categoria', 'tipoPessoa', 'ativo', 'acoes'];
  data = signal<TenantDto[]>([]);
  total = signal(0);
  pageIndex = signal(0);
  pageSize = signal(10);
  sortActive = signal<string>('nomeFantasia');
  sortDirection = signal<'asc'|'desc'>('asc');
  searchTerm = '';
  isMobile = signal(false);
  isLoading = signal(false);
  private searchTimeout: any;
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  constructor() {
    this.breakpointObserver.observe([Breakpoints.Handset, Breakpoints.TabletPortrait])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        this.isMobile.set(result.matches);
        this.cdr.markForCheck();
      });
    
    this.load();
  }

  onSearchChange() {
    if (this.searchTimeout) {
      clearTimeout(this.searchTimeout);
    }
    this.searchTimeout = setTimeout(() => {
      this.pageIndex.set(0);
      this.load();
    }, 300);
  }

  clearSearch() {
    this.searchTerm = '';
    this.pageIndex.set(0);
    this.load();
  }

  load() {
    this.isLoading.set(true);
    const page = this.pageIndex() + 1;
    const pageSize = this.pageSize();
    const sort = this.sortActive();
    const order = this.sortDirection();
    const search = this.searchTerm || undefined;
    
    this.service.getAll(search, page, pageSize, sort, order)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => {
          this.data.set(result.items);
          this.total.set(result.total);
          this.isLoading.set(false);
          this.cdr.markForCheck();
        },
        error: (err) => {
          this.toast.error(err.error?.message || 'Erro ao carregar tenants');
          this.isLoading.set(false);
          this.cdr.markForCheck();
        }
      });
  }

  pageChanged(event: PageEvent) {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  onSort(event: Sort) {
    this.sortActive.set(event.active);
    this.sortDirection.set(event.direction as 'asc' | 'desc');
    this.pageIndex.set(0);
    this.load();
  }

  edit(id: number) {
    this.router.navigate(['/backoffice/tenants/cadastro'], { state: { id } });
  }

  view(id: number) {
    this.router.navigate(['/backoffice/tenants/cadastro'], { state: { id, view: true } });
  }

  toggleActive(id: number, currentStatus: boolean) {
    const action = currentStatus ? 'inativar' : 'ativar';
    const warningMessage = currentStatus 
      ? 'O tenant nÃ£o poderÃ¡ mais acessar o sistema.'
      : undefined;

    this.confirmationService.confirmToggleActive(action, 'este tenant', warningMessage)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(confirmed => {
        if (!confirmed) return;

        this.service.toggleActive(id)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.toast.success(`Tenant ${action === 'inativar' ? 'inativado' : 'ativado'} com sucesso`);
              this.load();
            },
            error: (err) => {
              this.toast.error(err.error?.message || `Erro ao ${action} tenant`);
            }
          });
      });
  }
}

