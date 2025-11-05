import { Component, inject, signal, ViewChild } from "@angular/core";
import { CommonModule } from "@angular/common";
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
import { FormControl, ReactiveFormsModule } from "@angular/forms";
import { debounceTime, distinctUntilChanged } from "rxjs/operators";
import { ToastService } from "../../../services/toast.service";
import { PerfilService, PerfilDto } from "../../../services/perfil.service";

@Component({
  standalone: true,
  selector: 'app-perfis-list',
  imports: [CommonModule, RouterLink, ReactiveFormsModule, MatTableModule, MatButtonModule, MatIconModule, MatTooltipModule, MatSnackBarModule, MatPaginatorModule, MatSortModule, MatFormFieldModule, MatInputModule],
  templateUrl: './perfis-list.component.html',
  styleUrls: ['./perfis-list.component.scss']
})
export class PerfisListComponent {
  private service = inject(PerfilService);
  private router = inject(Router);
  private toast = inject(ToastService);
  displayedColumns = ['nome', 'ativo', 'acoes'];
  data = signal<PerfilDto[]>([]);
  total = signal(0);
  pageIndex = signal(0);
  pageSize = signal(10);
  sortActive = signal<string>('nome');
  sortDirection = signal<'asc'|'desc'>('asc');
  searchControl = new FormControl<string>('', { nonNullable: true });
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  @ViewChild(MatSort) sort!: MatSort;

  constructor() {
    this.searchControl.valueChanges.pipe(debounceTime(300), distinctUntilChanged()).subscribe(() => {
      this.pageIndex.set(0);
      this.load();
    });
    this.load();
  }

  load() {
    const page = this.pageIndex() + 1;
    const pageSize = this.pageSize();
    const sort = this.sortActive();
    const order = this.sortDirection();
    const search = this.searchControl.value || undefined;
    this.service.list({ page, pageSize, sort, order, search }).subscribe({
      next: res => { this.data.set(res.items); this.total.set(res.total); },
      error: () => this.toast.error('Falha ao carregar perfis')
    });
  }

  delete(id: number) {
    if (!confirm('Excluir perfil?')) return;
    this.service.delete(id).subscribe({ next: () => { this.toast.success('Perfil excluído'); this.load(); }, error: (e) => this.toast.error(e.error?.message || 'Falha ao excluir perfil') });
  }

  edit(e: PerfilDto) { this.router.navigate(['/perfis/cadastro'], { state: { id: e.id } }); }
  view(e: PerfilDto) { this.router.navigate(['/perfis/cadastro'], { state: { id: e.id, view: true } }); }

  pageChanged(ev: PageEvent) { this.pageIndex.set(ev.pageIndex); this.pageSize.set(ev.pageSize); this.load(); }
  onSort(ev: Sort) { this.sortActive.set(ev.active); this.sortDirection.set((ev.direction || 'asc') as any); this.load(); }
}
