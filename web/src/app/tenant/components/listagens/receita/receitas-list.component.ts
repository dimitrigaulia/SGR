import { Component, inject, signal, ViewChild, OnDestroy, ChangeDetectionStrategy, DestroyRef, ChangeDetectorRef } from "@angular/core";
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
import { MatDialog, MatDialogModule } from "@angular/material/dialog";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { filter } from "rxjs";
import { ToastService } from "../../../../core/services/toast.service";
import { ConfirmationService } from "../../../../core/services/confirmation.service";
import { ReceitaService, ReceitaDto } from "../../../../features/tenant-receitas/services/receita.service";
import { LoadingComponent } from "../../../../shared/components/loading/loading.component";
import { InputDialogComponent, InputDialogData } from "../../../../shared/components/input-dialog/input-dialog.component";
import { environment } from "../../../../../environments/environment";

@Component({
  standalone: true,
  selector: 'app-tenant-receitas-list',
  imports: [CommonModule, FormsModule, RouterLink, MatTableModule, MatButtonModule, MatIconModule, MatTooltipModule, MatSnackBarModule, MatPaginatorModule, MatSortModule, MatFormFieldModule, MatInputModule, MatCardModule, MatDialogModule, LoadingComponent],
  templateUrl: './receitas-list.component.html',
  styleUrls: ['./receitas-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TenantReceitasListComponent implements OnDestroy {
  private service = inject(ReceitaService);
  private router = inject(Router);
  private toast = inject(ToastService);
  private confirmationService = inject(ConfirmationService);
  private dialog = inject(MatDialog);
  private breakpointObserver = inject(BreakpointObserver);
  private destroyRef = inject(DestroyRef);
  private cdr = inject(ChangeDetectorRef);
  
  displayedColumns = ['imagem', 'nome', 'categoria', 'rendimento', 'custoPorPorcao', 'tempoPreparo', 'ativo', 'acoes'];
  data = signal<ReceitaDto[]>([]);
  total = signal(0);
  pageIndex = signal(0);
  pageSize = signal(10);
  sortActive = signal<string>('nome');
  sortDirection = signal<'asc'|'desc'>('asc');
  searchTerm = '';
  isMobile = signal(false);
  isLoading = signal(false);
  private searchTimeout: any;
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;
  
  // Propriedades para uso no template
  window = typeof window !== 'undefined' ? window : null;
  environment = environment;

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
          this.toast.error('Falha ao carregar receitas');
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

  formatTime(minutes: number | null | undefined): string {
    if (!minutes) return '-';
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    if (hours > 0) {
      return `${hours}h ${mins}min`;
    }
    return `${mins}min`;
  }

  delete(id: number) {
    this.confirmationService.confirmDelete('esta receita')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(confirmed => {
        if (!confirmed) return;

        this.isLoading.set(true);
        this.service.delete(id)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({ 
            next: () => { 
              this.toast.success('Receita excluÃ­da'); 
              this.load();
            }, 
            error: (e: any) => {
              this.toast.error(e.error?.message || 'Falha ao excluir receita');
              this.isLoading.set(false);
              this.cdr.markForCheck();
            }
          });
      });
  }

  edit(e: ReceitaDto) {
    this.router.navigate(['/tenant/receitas/cadastro'], { state: { id: e.id } });
  }

  view(e: ReceitaDto) {
    this.router.navigate(['/tenant/receitas/cadastro'], { state: { id: e.id, view: true } });
  }

  duplicar(e: ReceitaDto) {
    const dialogData: InputDialogData = {
      title: 'Duplicar Receita',
      message: `Digite o nome para a nova receita (baseada em "${e.nome}"):`,
      placeholder: 'Nome da receita',
      initialValue: `${e.nome} (CÃ³pia)`,
      confirmText: 'Duplicar',
      cancelText: 'Cancelar',
      required: true
    };

    const dialogRef = this.dialog.open(InputDialogComponent, {
      width: '400px',
      data: dialogData,
      disableClose: false,
      autoFocus: true
    });

    dialogRef.afterClosed()
      .pipe(
        filter((result): result is string => result !== null && result !== undefined && result.trim() !== ''),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(novoNome => {
        if (!novoNome || !novoNome.trim()) return;

        this.isLoading.set(true);
        this.service.duplicar(e.id, novoNome.trim())
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.toast.success('Receita duplicada com sucesso');
              this.load();
            },
            error: (err: any) => {
              this.toast.error(err.error?.message || 'Falha ao duplicar receita');
              this.isLoading.set(false);
              this.cdr.markForCheck();
            }
          });
      });
  }

  printPdf(e: ReceitaDto) {
    if (this.window) {
      this.window.open(`${this.environment.apiUrl}/tenant/receitas/${e.id}/pdf`, '_blank');
    }
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

  ngOnDestroy() {
    if (this.searchTimeout) {
      clearTimeout(this.searchTimeout);
    }
  }
}

