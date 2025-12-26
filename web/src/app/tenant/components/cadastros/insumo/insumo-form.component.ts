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
import { MatCardModule } from '@angular/material/card';
import { InsumoService, CreateInsumoRequest, UpdateInsumoRequest, InsumoDto } from '../../../../features/tenant-insumos/services/insumo.service';
import { CategoriaInsumoService, CategoriaInsumoDto } from '../../../../features/tenant-categorias-insumo/services/categoria-insumo.service';
import { UnidadeMedidaService, UnidadeMedidaDto } from '../../../../features/tenant-unidades-medida/services/unidade-medida.service';
import { ToastService } from '../../../../core/services/toast.service';
import { UploadService } from '../../../../features/usuarios/services/upload.service';

type InsumoFormModel = Omit<InsumoDto, 'id' | 'categoriaNome' | 'unidadeCompraNome' | 'unidadeCompraSigla' | 'unidadeUsoNome' | 'unidadeUsoSigla' | 'quantidadeAjustadaIPC' | 'custoPorUnidadeUsoAlternativo'>;

@Component({
  standalone: true,
  selector: 'app-tenant-insumo-form',
  imports: [CommonModule, FormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule, MatSelectModule, MatSlideToggleModule, MatSnackBarModule, MatCardModule],
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
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

  model: InsumoFormModel = {
    nome: '',
    categoriaId: null as any,
    unidadeCompraId: null as any,
    unidadeUsoId: null as any,
    quantidadePorEmbalagem: 1,
    custoUnitario: 0,
    fatorCorrecao: 1.0,
    ipcValor: 0,
    descricao: '',
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
            unidadeCompraId: e.unidadeCompraId,
            unidadeUsoId: e.unidadeUsoId,
            quantidadePorEmbalagem: e.quantidadePorEmbalagem,
            custoUnitario: e.custoUnitario,
            fatorCorrecao: e.fatorCorrecao,
            ipcValor: e.ipcValor ?? 0,
            descricao: e.descricao || '',
            pathImagem: e.pathImagem || '',
            isAtivo: e.isAtivo
          };
          this.previousImageUrl = e.pathImagem ?? null;
          this.cdr.markForCheck();
        });
    }
  }

  private findUnidade(id: number | null | undefined): UnidadeMedidaDto | undefined {
    if (!id) return undefined;
    return this.unidades().find(u => u.id === id);
  }

  get unidadeCompraSelecionada(): UnidadeMedidaDto | undefined {
    return this.findUnidade(this.model.unidadeCompraId);
  }

  get unidadeUsoSelecionada(): UnidadeMedidaDto | undefined {
    return this.findUnidade(this.model.unidadeUsoId);
  }

  get tiposUnidadeMensagem(): string {
    const compra = this.unidadeCompraSelecionada;
    const uso = this.unidadeUsoSelecionada;

    if (!compra || !uso) {
      return 'Escolha primeiro as unidades de compra e de uso para ver como o sistema farÃ¡ a conversÃ£o automÃ¡tica.';
    }

    // Com unidades simplificadas, não há mais conversão automática por tipo
    return `Atenção: a Quantidade por Embalagem deve estar na mesma unidade de uso (${uso.sigla}) para que o custo fique correto.`;
  }

  get resumoCustoPorUnidadeCompra(): string {
    const unidadeCompra = this.findUnidade(this.model.unidadeCompraId);
    const unidadeUso = this.findUnidade(this.model.unidadeUsoId);
    const quantidadePorEmbalagem = this.model.quantidadePorEmbalagem;
    const custo = this.model.custoUnitario || 0;
    
    if (!unidadeCompra || custo <= 0) {
      return '-';
    }

    // Se unidade de compra = unidade de uso, mostrar custo da embalagem completa
    // Ex: "R$ 10,00 / 1000 g" ao invés de "R$ 10,00 / GR"
    if (unidadeCompra.id === unidadeUso?.id && quantidadePorEmbalagem > 0) {
      const valor = custo.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
      return `${valor} / ${quantidadePorEmbalagem.toLocaleString('pt-BR', { minimumFractionDigits: 0, maximumFractionDigits: 4 })} ${unidadeCompra.sigla}`;
    }

    // Caso contrário, manter comportamento original
    const valor = custo.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
    return `${valor} / ${unidadeCompra.sigla}`;
  }

  get resumoCustoPorUnidadeUso(): string {
    const unidadeCompra = this.findUnidade(this.model.unidadeCompraId);
    const unidadeUso = this.findUnidade(this.model.unidadeUsoId);
    const quantidadePorEmbalagem = this.model.quantidadePorEmbalagem;
    const custo = this.model.custoUnitario || 0;
    const ipcValor = this.model.ipcValor || 0;

    if (!unidadeCompra || !unidadeUso || quantidadePorEmbalagem <= 0 || custo <= 0) {
      return '-';
    }

    // Fórmula alternativa: CustoUnitario * (QuantidadePorEmbalagem / IPCValor)
    let quantidadeAjustada = quantidadePorEmbalagem;
    if (ipcValor > 0) {
      quantidadeAjustada = quantidadePorEmbalagem / ipcValor;
    }

    const custoPorUnidadeUso = custo * quantidadeAjustada;
    const valor = custoPorUnidadeUso.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
    return `${valor} / ${unidadeUso.sigla}`;
  }

  save() {
    this.error.set('');
    if (this.isView()) return;
    const v = this.model;
    // Validação simples
    if (!v.nome || !v.categoriaId || !v.unidadeCompraId || !v.unidadeUsoId || !v.quantidadePorEmbalagem) {
      this.toast.error('Preencha os campos obrigatórios corretamente');
      return;
    }

    if (!this.isEdit()) {
      const req: CreateInsumoRequest = {
        nome: v.nome,
        categoriaId: v.categoriaId!,
        unidadeCompraId: v.unidadeCompraId!,
        unidadeUsoId: v.unidadeUsoId!,
        quantidadePorEmbalagem: v.quantidadePorEmbalagem,
        custoUnitario: v.custoUnitario || 0,
        fatorCorrecao: v.fatorCorrecao || 1.0,
        ipcValor: v.ipcValor && v.ipcValor > 0 ? v.ipcValor : undefined,
        descricao: v.descricao || undefined,
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
        unidadeCompraId: v.unidadeCompraId!,
        unidadeUsoId: v.unidadeUsoId!,
        quantidadePorEmbalagem: v.quantidadePorEmbalagem,
        custoUnitario: v.custoUnitario || 0,
        fatorCorrecao: v.fatorCorrecao || 1.0,
        ipcValor: v.ipcValor && v.ipcValor > 0 ? v.ipcValor : undefined,
        descricao: v.descricao || undefined,
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

}

