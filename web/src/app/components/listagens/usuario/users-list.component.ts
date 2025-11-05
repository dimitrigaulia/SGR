import { Component, inject, signal, ViewChild, OnDestroy } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { Router, RouterLink } from "@angular/router";
import { MatTableModule } from "@angular/material/table";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatTooltipModule } from "@angular/material/tooltip";
import { MatSnackBarModule } from "@angular/material/snack-bar";
import { MatPaginator, MatPaginatorModule, PageEvent } from "@angular/material/paginator";
import { MatSort, MatSortModule, Sort } from "@angular/material/sort";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { ToastService } from "../../../services/toast.service";
import { UsuarioService, UsuarioDto } from "../../../services/usuario.service";

@Component({
  standalone: true,
  selector: 'app-users-list',
  imports: [CommonModule, FormsModule, RouterLink, MatTableModule, MatButtonModule, MatIconModule, MatTooltipModule, MatSnackBarModule, MatPaginatorModule, MatSortModule, MatFormFieldModule, MatInputModule],
  templateUrl: './users-list.component.html',
  styleUrls: ['./users-list.component.scss']
})
export class UsersListComponent implements OnDestroy {
  private service = inject(UsuarioService);
  private router = inject(Router);
  private toast = inject(ToastService);
  displayedColumns = ['avatar', 'nome', 'email', 'ativo', 'acoes'];
  data = signal<UsuarioDto[]>([]);
  total = signal(0);
  pageIndex = signal(0);
  pageSize = signal(10);
  sortActive = signal<string>('nome');
  sortDirection = signal<'asc'|'desc'>('asc');
  searchTerm = '';
  private searchTimeout: any;
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  constructor() {
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
    const page = this.pageIndex() + 1;
    const pageSize = this.pageSize();
    const sort = this.sortActive();
    const order = this.sortDirection();
    const search = this.searchTerm || undefined;
    this.service.list({ page, pageSize, sort, order, search }).subscribe({
      next: res => { this.data.set(res.items); this.total.set(res.total); },
      error: () => this.toast.error('Falha ao carregar usuários')
    });
  }

  clearSearch() {
    this.searchTerm = '';
    this.pageIndex.set(0);
    this.load();
  }

  delete(id: number) {
    if (!confirm('Excluir usuário?')) return;
    this.service.delete(id).subscribe({ next: () => { this.toast.success('Usuário excluído'); this.load(); }, error: (e) => this.toast.error(e.error?.message || 'Falha ao excluir usuário') });
  }

  edit(e: UsuarioDto) {
    this.router.navigate(['/usuarios/cadastro'], { state: { id: e.id } });
  }

  view(e: UsuarioDto) {
    this.router.navigate(['/usuarios/cadastro'], { state: { id: e.id, view: true } });
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
