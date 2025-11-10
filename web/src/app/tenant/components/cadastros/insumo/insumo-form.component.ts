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
import { InsumoService, CreateInsumoRequest, UpdateInsumoRequest, InsumoDto } from '../../../../features/tenant-insumos/services/insumo.service';
import { CategoriaInsumoService, CategoriaInsumoDto } from '../../../../features/tenant-categorias-insumo/services/categoria-insumo.service';
import { UnidadeMedidaService, UnidadeMedidaDto } from '../../../../features/tenant-unidades-medida/services/unidade-medida.service';
import { ToastService } from '../../../../core/services/toast.service';
import { UploadService } from '../../../../features/usuarios/services/upload.service';

type InsumoFormModel = Omit<InsumoDto, 'id' | 'categoriaNome' | 'unidadeMedidaNome' | 'unidadeMedidaSigla'>;

@Component({
  standalone: true,
  selector: 'app-tenant-insumo-form',
  imports: [CommonModule, FormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule, MatSelectModule, MatSlideToggleModule, MatSnackBarModule],
  templateUrl: './insumo-form.component.html',
  styleUrls: ['./insumo-form.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TenantInsumoFormComponent {
  private router = inject(Router);
  private service = inject(InsumoService);
  private categoriaService = inject(CategoriaInsumoService);
  private unidadeService = inject(UnidadeMedidaService);
  private toast = inject(ToastService);
  private upload = inject(UploadService);
  private cdr = inject(ChangeDetectorRef);
  private destroyRef = inject(DestroyRef);

  id = signal<number | null>(null);
  categorias = signal<CategoriaInsumoDto[]>([]);
  unidades = signal<UnidadeMedidaDto[]>([]);
  isEdit = computed(() => this.id() !== null);
  isView = signal<boolean>(false);
  error = signal<string>('');
  previousImageUrl: string | null = null;
  codigoBarrasTaken = false;
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

  model: InsumoFormModel = {
    nome: '',
    categoriaId: null as any,
    unidadeMedidaId: null as any,
    custoUnitario: 0,
    estoqueMinimo: null,
    descricao: '',
    codigoBarras: '',
    pathImagem: '',
    isAtivo: true
  };

  constructor() {
    // Carregar categorias e unidades
    this.categoriaService.list({ pageSize: 1000 })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ 
        next: res => {
          this.categorias.set(res.items);
          this.cdr.markForCheck();
        }
      });

    this.unidadeService.list({ pageSize: 1000 })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ 
        next: res => {
          this.unidades.set(res.items);
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
          this.model = { 
            nome: e.nome,
            categoriaId: e.categoriaId,
            unidadeMedidaId: e.unidadeMedidaId,
            custoUnitario: e.custoUnitario,
            estoqueMinimo: e.estoqueMinimo,
            descricao: e.descricao || '',
            codigoBarras: e.codigoBarras || '',
            pathImagem: e.pathImagem || '',
            isAtivo: e.isAtivo
          };
          this.previousImageUrl = e.pathImagem ?? null;
          this.cdr.markForCheck();
        });
    }
  }

  save() {
    this.error.set('');
    if (this.isView()) return;
    const v = this.model;
    // Validação simples
    if (!v.nome || !v.categoriaId || !v.unidadeMedidaId || this.codigoBarrasTaken) {
      this.toast.error('Preencha os campos obrigatórios corretamente');
      return;
    }

    if (!this.isEdit()) {
      const req: CreateInsumoRequest = {
        nome: v.nome,
        categoriaId: v.categoriaId!,
        unidadeMedidaId: v.unidadeMedidaId!,
        custoUnitario: v.custoUnitario || 0,
        estoqueMinimo: v.estoqueMinimo || undefined,
        descricao: v.descricao || undefined,
        codigoBarras: v.codigoBarras || undefined,
        pathImagem: v.pathImagem || undefined,
        isAtivo: !!v.isAtivo
      };
      this.service.create(req)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => { this.toast.success('Insumo criado'); this.router.navigate(['/tenant/insumos']); },
          error: (err) => { 
            const msg = err.error?.message || 'Erro ao salvar insumo'; 
            this.toast.error(msg); 
            this.error.set(msg);
            this.cdr.markForCheck();
          }
        });
    } else {
      const req: UpdateInsumoRequest = {
        nome: v.nome,
        categoriaId: v.categoriaId!,
        unidadeMedidaId: v.unidadeMedidaId!,
        custoUnitario: v.custoUnitario || 0,
        estoqueMinimo: v.estoqueMinimo || undefined,
        descricao: v.descricao || undefined,
        codigoBarras: v.codigoBarras || undefined,
        pathImagem: v.pathImagem || undefined,
        isAtivo: !!v.isAtivo
      };
      this.service.update(this.id()!, req)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            const newUrl = req.pathImagem || '';
            if (this.previousImageUrl && this.previousImageUrl !== newUrl && this.previousImageUrl.includes('/avatars/')) {
              this.upload.deleteAvatar(this.previousImageUrl)
                .pipe(takeUntilDestroyed(this.destroyRef))
                .subscribe({ next: () => {}, error: () => {} });
            }
            this.previousImageUrl = newUrl || null;
            this.toast.success('Insumo atualizado');
            this.router.navigate(['/tenant/insumos']);
          },
          error: (err) => { 
            const msg = err.error?.message || 'Erro ao salvar insumo'; 
            this.toast.error(msg); 
            this.error.set(msg);
            this.cdr.markForCheck();
          }
        });
    }
  }

  triggerFile() { this.fileInput?.nativeElement.click(); }

  clearImage() {
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
          this.toast.success('Imagem atualizada');
          this.cdr.markForCheck();
        },
        error: () => { 
          this.toast.error('Falha ao enviar imagem');
          this.cdr.markForCheck();
        }
      });
  }

  onCodigoBarrasBlur(value: string) {
    if (!value) { 
      this.codigoBarrasTaken = false; 
      this.cdr.markForCheck();
      return; 
    }
    const excludeId = this.id() ?? undefined;
    this.service.checkCodigoBarras(value, excludeId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ 
        next: res => {
          this.codigoBarrasTaken = res.exists;
          this.cdr.markForCheck();
        }, 
        error: () => {
          this.codigoBarrasTaken = false;
          this.cdr.markForCheck();
        }
      });
  }
}

