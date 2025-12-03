import { Component, computed, inject, signal, ChangeDetectionStrategy, ChangeDetectorRef, DestroyRef } from '@angular/core';
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
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { ReceitaService, CreateReceitaRequest, UpdateReceitaRequest, ReceitaDto, CreateReceitaItemRequest, UpdateReceitaItemRequest } from '../../../../features/tenant-receitas/services/receita.service';
import { CategoriaReceitaService, CategoriaReceitaDto } from '../../../../features/tenant-categorias-receita/services/categoria-receita.service';
import { InsumoService, InsumoDto } from '../../../../features/tenant-insumos/services/insumo.service';
import { UnidadeMedidaService, UnidadeMedidaDto } from '../../../../features/tenant-unidades-medida/services/unidade-medida.service';
import { ToastService } from '../../../../core/services/toast.service';
import { UploadService, UploadResponse } from '../../../../features/usuarios/services/upload.service';
import { MatCardModule } from '@angular/material/card';
import { environment } from '../../../../../environments/environment';

type ReceitaItemFormModel = {
  insumoId: number | null;
  quantidade: number;
  unidadeMedidaId: number | null;
  exibirComoQB: boolean;
  ordem: number;
  observacoes: string;
  custoItem?: number;
  custoPorUnidadeUso?: number | null;
  custoPor100UnidadesUso?: number | null;
};

@Component({
  standalone: true,
  selector: 'app-tenant-receita-form',
  imports: [CommonModule, FormsModule, MatCardModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule, MatSelectModule, MatSlideToggleModule, MatSnackBarModule, MatTableModule, MatIconModule, MatCheckboxModule],
  templateUrl: './receita-form.component.html',
  styleUrls: ['./receita-form.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TenantReceitaFormComponent {
  private router = inject(Router);
  private service = inject(ReceitaService);
  private categoriaService = inject(CategoriaReceitaService);
  private insumoService = inject(InsumoService);
  private unidadeService = inject(UnidadeMedidaService);
  private toast = inject(ToastService);
  private upload = inject(UploadService);
  private cdr = inject(ChangeDetectorRef);
  private destroyRef = inject(DestroyRef);

  id = signal<number | null>(null);
  categorias = signal<CategoriaReceitaDto[]>([]);
  insumos = signal<InsumoDto[]>([]);
  unidades = signal<UnidadeMedidaDto[]>([]);
  isEdit = computed(() => this.id() !== null);
  isView = signal<boolean>(false);
  error = signal<string>('');
  previousImageUrl: string | null = null;
  
  model = {
    nome: '',
    categoriaId: null as number | null,
    descricao: '',
    instrucoesEmpratamento: '',
    rendimento: 1,
    pesoPorPorcao: null as number | null,
    toleranciaPeso: null as number | null,
    fatorRendimento: 1.0,
    icSinal: '-' as string | null,
    icValor: 0 as number | null,
    tempoPreparo: null as number | null,
    versao: '1.0',
    pathImagem: '',
    isAtivo: true
  };

  itens = signal<ReceitaItemFormModel[]>([]);
  displayedColumns = ['ordem', 'insumo', 'quantidade', 'unidade', 'qb', 'observacoes', 'acoes'];
  
  // Propriedades para uso no template
  window = typeof window !== 'undefined' ? window : null;
  environment = environment;

  get fatorRendimentoCalculado(): number {
    const sinal = this.model.icSinal || '-';
    const v = this.model.icValor ?? 0;
    const delta = Math.max(0, Math.min(999, v)) / 100;
    if (delta === 0) return 1.0;
    return sinal === '-' ? 1 - delta : 1 + delta;
  }

  constructor() {
    // Carregar categorias e insumos
    this.categoriaService.list({ pageSize: 1000 })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ 
        next: res => {
          this.categorias.set(res.items);
          this.cdr.markForCheck();
        }
      });

    this.insumoService.list({ pageSize: 1000 })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ 
        next: res => {
          this.insumos.set(res.items.filter(i => i.isAtivo));
          this.cdr.markForCheck();
        }
      });

    this.unidadeService.list({ pageSize: 1000 })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({ 
        next: res => {
          this.unidades.set(res.items.filter(u => u.isAtivo));
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
            descricao: e.descricao || '',
            instrucoesEmpratamento: e.instrucoesEmpratamento || '',
            rendimento: e.rendimento,
            pesoPorPorcao: e.pesoPorPorcao ?? null,
            toleranciaPeso: e.toleranciaPeso ?? null,
            fatorRendimento: e.fatorRendimento,
            icSinal: e.icSinal ?? '-',
            icValor: e.icValor ?? 0,
            tempoPreparo: e.tempoPreparo ?? null,
            versao: e.versao || '1.0',
            pathImagem: e.pathImagem || '',
            isAtivo: e.isAtivo
          };
          this.previousImageUrl = e.pathImagem ?? null;
          this.itens.set(e.itens.map(item => ({
            insumoId: item.insumoId,
            quantidade: item.quantidade,
            unidadeMedidaId: item.unidadeMedidaId,
            exibirComoQB: item.exibirComoQB,
            ordem: item.ordem,
            observacoes: item.observacoes || '',
            custoItem: item.custoItem,
            custoPorUnidadeUso: item.custoPorUnidadeUso ?? null,
            custoPor100UnidadesUso: item.custoPor100UnidadesUso ?? null
          })));
          this.cdr.markForCheck();
        });
    } else {
      // Adicionar um item vazio inicial
      this.addItem();
    }
  }

  addItem() {
    const currentItens = this.itens();
    const newOrdem = currentItens.length > 0 ? Math.max(...currentItens.map(i => i.ordem)) + 1 : 1;
    this.itens.set([...currentItens, {
      insumoId: null,
      quantidade: 0,
      unidadeMedidaId: null,
      exibirComoQB: false,
      ordem: newOrdem,
      observacoes: ''
    }]);
    this.cdr.markForCheck();
  }

  removeItem(index: number) {
    const currentItens = this.itens();
    const newItens = currentItens.filter((_, i) => i !== index);
    // Reordenar
    newItens.forEach((item, idx) => {
      item.ordem = idx + 1;
    });
    this.itens.set(newItens);
    this.cdr.markForCheck();
  }

  moveItemUp(index: number) {
    if (index === 0) return;
    const currentItens = [...this.itens()];
    [currentItens[index - 1], currentItens[index]] = [currentItens[index], currentItens[index - 1]];
    currentItens.forEach((item, idx) => {
      item.ordem = idx + 1;
    });
    this.itens.set(currentItens);
    this.cdr.markForCheck();
  }

  moveItemDown(index: number) {
    const currentItens = this.itens();
    if (index === currentItens.length - 1) return;
    const newItens = [...currentItens];
    [newItens[index], newItens[index + 1]] = [newItens[index + 1], newItens[index]];
    newItens.forEach((item, idx) => {
      item.ordem = idx + 1;
    });
    this.itens.set(newItens);
    this.cdr.markForCheck();
  }

  getInsumoNome(insumoId: number | null): string {
    if (!insumoId) return '';
    const insumo = this.insumos().find(i => i.id === insumoId);
    return insumo ? insumo.nome : '';
  }

  getUnidadeSigla(unidadeMedidaId: number | null): string {
    if (!unidadeMedidaId) return '';
    const unidade = this.unidades().find(u => u.id === unidadeMedidaId);
    return unidade ? unidade.sigla : '';
  }

  getStepForUnidade(unidadeMedidaId: number | null): string {
    if (!unidadeMedidaId) return '0.0001';
    const unidade = this.unidades().find(u => u.id === unidadeMedidaId);
    if (!unidade) return '0.0001';
    // Se for UN (Unidade), usar step 1, caso contrário 0.0001
    if (unidade.sigla.toUpperCase() === 'UN') {
      return '1';
    }
    return '0.0001';
  }

  formatQuantidade(quantidade: number, unidadeMedidaId: number | null): string {
    const unidade = this.unidades().find(u => u.id === unidadeMedidaId);
    if (unidade && unidade.sigla.toUpperCase() === 'UN') {
      return Math.round(quantidade).toString();
    }
    // Para outras unidades, mostrar até 4 casas decimais
    return quantidade.toFixed(4).replace(/\.?0+$/, '');
  }

  onInsumoChange(item: ReceitaItemFormModel, index: number) {
    // Quando o insumo muda, pode ajustar a unidade de medida padrão se necessário
    // Por enquanto, apenas marca para check
    this.cdr.markForCheck();
  }

  get pesoTotalTeorico(): number | null {
    const pesoPorPorcao = this.model.pesoPorPorcao;
    const rendimento = this.model.rendimento;
    if (pesoPorPorcao && rendimento > 0) {
      return pesoPorPorcao * rendimento;
    }
    return null;
  }

  formatCurrency(value: number | null | undefined): string {
    if (value === null || value === undefined) {
      return '-';
    }
    return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);
  }

  onQuantidadeChange(item: ReceitaItemFormModel, index: number) {
    // Quando a quantidade muda, ajustar se for unidade UN
    if (item.unidadeMedidaId) {
      const unidade = this.unidades().find(u => u.id === item.unidadeMedidaId);
      if (unidade && unidade.sigla.toUpperCase() === 'UN' && item.quantidade > 0) {
        // Arredondar para inteiro
        const rounded = Math.round(item.quantidade);
        if (rounded !== item.quantidade) {
          item.quantidade = rounded;
          if (item.quantidade < 1) {
            item.quantidade = 1;
          }
          this.cdr.markForCheck();
        }
      }
    }
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;
    const file = input.files[0];
    
    // Validar tipo de arquivo
    const valid = ['image/png', 'image/jpeg'].includes(file.type);
    if (!valid) { 
      this.toast.error('Apenas imagens PNG ou JPG'); 
      input.value = ''; 
      return; 
    }
    
    this.upload.uploadAvatar(file)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.model.pathImagem = res.url;
          this.toast.success('Imagem atualizada');
          this.cdr.markForCheck();
        },
        error: () => {
          this.toast.error('Erro ao fazer upload da imagem');
          this.cdr.markForCheck();
        }
      });
  }

  save() {
    this.error.set('');
    if (this.isView()) return;
    
    const v = this.model;
    if (!v.nome || !v.categoriaId || v.rendimento <= 0) {
      this.toast.error('Preencha os campos obrigatórios corretamente');
      return;
    }

    const validItens = this.itens().filter(item => 
      item.insumoId !== null && 
      item.quantidade > 0 && 
      item.unidadeMedidaId !== null
    );
    if (validItens.length === 0) {
      this.toast.error('Adicione pelo menos um item válido à receita (com insumo, quantidade e unidade de medida)');
      return;
    }

    // Arredondar quantidades baseado na unidade de medida antes de enviar
    const itensRequest = validItens.map(item => {
      let quantidade = item.quantidade;
      
      // Se for unidade UN, garantir que seja inteiro
      if (item.unidadeMedidaId) {
        const unidade = this.unidades().find(u => u.id === item.unidadeMedidaId);
        if (unidade && unidade.sigla.toUpperCase() === 'UN') {
          quantidade = Math.round(quantidade);
          if (quantidade < 1) quantidade = 1;
        }
      }
      
      return {
        insumoId: item.insumoId!,
        quantidade: quantidade,
        unidadeMedidaId: item.unidadeMedidaId!,
        exibirComoQB: item.exibirComoQB,
        ordem: item.ordem,
        observacoes: item.observacoes || undefined
      };
    });

    if (this.id() === null) {
      const req: CreateReceitaRequest = {
        nome: v.nome,
        categoriaId: v.categoriaId!,
        descricao: v.descricao || undefined,
        instrucoesEmpratamento: v.instrucoesEmpratamento || undefined,
        rendimento: v.rendimento,
        pesoPorPorcao: v.pesoPorPorcao || undefined,
        toleranciaPeso: v.toleranciaPeso || undefined,
        fatorRendimento: this.fatorRendimentoCalculado || 1.0,
        icSinal: v.icSinal || undefined,
        icValor: v.icValor ?? undefined,
        tempoPreparo: v.tempoPreparo || undefined,
        versao: v.versao || '1.0',
        pathImagem: v.pathImagem || undefined,
        itens: itensRequest as CreateReceitaItemRequest[],
        isAtivo: !!v.isAtivo
      };
      this.service.create(req)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => { 
            this.toast.success('Receita criada'); 
            this.router.navigate(['/tenant/receitas']); 
          },
          error: err => { 
            const msg = err.error?.message || 'Erro ao salvar receita'; 
            this.toast.error(msg); 
            this.error.set(msg);
            this.cdr.markForCheck();
          }
        });
    } else {
      const req: UpdateReceitaRequest = {
        nome: v.nome,
        categoriaId: v.categoriaId!,
        descricao: v.descricao || undefined,
        instrucoesEmpratamento: v.instrucoesEmpratamento || undefined,
        rendimento: v.rendimento,
        pesoPorPorcao: v.pesoPorPorcao || undefined,
        toleranciaPeso: v.toleranciaPeso || undefined,
        fatorRendimento: this.fatorRendimentoCalculado || 1.0,
        icSinal: v.icSinal || undefined,
        icValor: v.icValor ?? undefined,
        tempoPreparo: v.tempoPreparo || undefined,
        versao: v.versao || '1.0',
        pathImagem: v.pathImagem || undefined,
        itens: itensRequest as UpdateReceitaItemRequest[],
        isAtivo: !!v.isAtivo
      };
      this.service.update(this.id()!, req)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => { 
            this.toast.success('Receita atualizada'); 
            this.router.navigate(['/tenant/receitas']); 
          },
          error: err => { 
            const msg = err.error?.message || 'Erro ao salvar receita'; 
            this.toast.error(msg); 
            this.error.set(msg);
            this.cdr.markForCheck();
          }
        });
    }
  }
}

