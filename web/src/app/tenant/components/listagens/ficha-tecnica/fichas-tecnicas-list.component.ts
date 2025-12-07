import { Component, inject, signal, ViewChild, ChangeDetectionStrategy, DestroyRef, ChangeDetectorRef } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { Router, RouterLink } from "@angular/router";
import { HttpClient } from "@angular/common/http";
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
import { MatDialogModule } from "@angular/material/dialog";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { ToastService } from "../../../../core/services/toast.service";
import { ConfirmationService } from "../../../../core/services/confirmation.service";
import { FichaTecnicaService, FichaTecnicaDto } from "../../../../features/tenant-receitas/services/ficha-tecnica.service";
import { LoadingComponent } from "../../../../shared/components/loading/loading.component";
import { environment } from "../../../../../environments/environment";

@Component({
  standalone: true,
  selector: 'app-tenant-fichas-tecnicas-list',
  imports: [CommonModule, FormsModule, RouterLink, MatTableModule, MatButtonModule, MatIconModule, MatTooltipModule, MatSnackBarModule, MatPaginatorModule, MatSortModule, MatFormFieldModule, MatInputModule, MatCardModule, MatDialogModule, LoadingComponent],
  templateUrl: './fichas-tecnicas-list.component.html',
  styleUrls: ['./fichas-tecnicas-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TenantFichasTecnicasListComponent {
  private service = inject(FichaTecnicaService);
  private router = inject(Router);
  private toast = inject(ToastService);
  private confirmationService = inject(ConfirmationService);
  private breakpointObserver = inject(BreakpointObserver);
  private destroyRef = inject(DestroyRef);
  private cdr = inject(ChangeDetectorRef);
  private http = inject(HttpClient);
  
  protected environment = environment;
  protected window = typeof window !== 'undefined' ? window : null;

  displayedColumns = ['nome', 'categoria', 'codigo', 'custoPorUnidade', 'precoSugerido', 'canais', 'ativo', 'acoes'];
  data = signal<FichaTecnicaDto[]>([]);
  total = signal(0);
  pageIndex = signal(0);
  pageSize = signal(10);
  sortActive = signal<string>('nome');
  sortDirection = signal<'asc' | 'desc'>('asc');
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

  load() {
    this.isLoading.set(true);
    const page = this.pageIndex() + 1;
    const pageSize = this.pageSize();
    const sort = this.sortActive();
    const order = this.sortDirection();
    const search = this.searchTerm || undefined;

    this.service.list({ page, pageSize, sort, order, search })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.data.set(res.items);
          this.total.set(res.total);
          this.isLoading.set(false);
          this.cdr.markForCheck();
        },
        error: () => {
          this.toast.error('Falha ao carregar fichas tÃ©cnicas');
          this.isLoading.set(false);
          this.cdr.markForCheck();
        }
      });
  }

  clearSearch() {
    this.searchTerm = '';
    this.pageIndex.set(0);
    this.load();
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);
  }

  delete(id: number) {
    this.confirmationService.confirmDelete('esta ficha tÃ©cnica')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(confirmed => {
        if (!confirmed) return;

        this.isLoading.set(true);
        this.service.delete(id)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.toast.success('Ficha tÃ©cnica excluÃ­da');
              this.load();
            },
            error: (e: any) => {
              this.toast.error(e.error?.message || 'Falha ao excluir ficha tÃ©cnica');
              this.isLoading.set(false);
              this.cdr.markForCheck();
            }
          });
      });
  }

  edit(e: FichaTecnicaDto) {
    this.router.navigate(['/tenant/fichas-tecnicas/cadastro'], { state: { id: e.id } });
  }

  view(e: FichaTecnicaDto) {
    this.router.navigate(['/tenant/fichas-tecnicas/cadastro'], { state: { id: e.id, view: true } });
  }

  pageChanged(ev: PageEvent) {
    this.pageIndex.set(ev.pageIndex);
    this.pageSize.set(ev.pageSize);
    this.load();
  }

  onSort(ev: Sort) {
    this.sortActive.set(ev.active);
    this.sortDirection.set((ev.direction || 'asc') as any);
    this.load();
  }

  printPdf(e: FichaTecnicaDto) {
    this.http.get(`${this.environment.apiUrl}/tenant/fichas-tecnicas/${e.id}/pdf`, {
      responseType: 'blob'
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (blob) => {
          const url = window.URL.createObjectURL(blob);
          const link = window.open(url, '_blank');
          if (link) {
            link.onload = () => window.URL.revokeObjectURL(url);
          }
        },
        error: (err) => {
          this.toast.error('Erro ao gerar PDF');
        }
      });
  }
}

