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
import { ReceitaService, CreateReceitaRequest, UpdateReceitaRequest, ReceitaDto, CreateReceitaItemRequest, UpdateReceitaItemRequest } from '../../../../features/tenant-receitas/services/receita.service';
import { CategoriaReceitaService, CategoriaReceitaDto } from '../../../../features/tenant-categorias-receita/services/categoria-receita.service';
import { InsumoService, InsumoDto } from '../../../../features/tenant-insumos/services/insumo.service';
import { ToastService } from '../../../../core/services/toast.service';
import { UploadService, UploadResponse } from '../../../../features/usuarios/services/upload.service';
import { MatCardModule } from '@angular/material/card';

type ReceitaItemFormModel = {
  insumoId: number | null;
  quantidade: number;
  ordem: number;
  observacoes: string;
  custoItem?: number;
  custoPorUnidadeUso?: number | null;
  custoPor100UnidadesUso?: number | null;
};

@Component({
  standalone: true,
  selector: 'app-tenant-receita-form',
  imports: [CommonModule, FormsModule, MatCardModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule, MatSelectModule, MatSlideToggleModule, MatSnackBarModule, MatTableModule, MatIconModule],
  templateUrl: './receita-form.component.html',
  styleUrls: ['./receita-form.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TenantReceitaFormComponent {
  private router = inject(Router);
  private service = inject(ReceitaService);
  private categoriaService = inject(CategoriaReceitaService);
  private insumoService = inject(InsumoService);
  private toast = inject(ToastService);
  private upload = inject(UploadService);
  private cdr = inject(ChangeDetectorRef);
  private destroyRef = inject(DestroyRef);

  id = signal<number | null>(null);
  categorias = signal<CategoriaReceitaDto[]>([]);
  insumos = signal<InsumoDto[]>([]);
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
    tempoPreparo: null as number | null,
    versao: '1.0',
    pathImagem: '',
    isAtivo: true
  };

  itens = signal<ReceitaItemFormModel[]>([]);
  displayedColumns = ['ordem', 'insumo', 'quantidade', 'observacoes', 'acoes'];

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
            tempoPreparo: e.tempoPreparo ?? null,
            versao: e.versao || '1.0',
            pathImagem: e.pathImagem || '',
            isAtivo: e.isAtivo
          };
          this.previousImageUrl = e.pathImagem ?? null;
          this.itens.set(e.itens.map(item => ({
            insumoId: item.insumoId,
            quantidade: item.quantidade,
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

  getInsumoUnidade(insumoId: number | null): string {
    if (!insumoId) return '';
    const insumo = this.insumos().find(i => i.id === insumoId);
    return insumo ? (insumo.unidadeUsoSigla || insumo.unidadeUsoNome || '') : '';
  }

  getInsumoUnidadeTipo(insumoId: number | null): string | null {
    if (!insumoId) return null;
    const insumo = this.insumos().find(i => i.id === insumoId);
    return insumo ? (insumo.unidadeUsoTipo || null) : null;
  }

  getStepForUnidade(insumoId: number | null): string {
    const tipo = this.getInsumoUnidadeTipo(insumoId);
    if (tipo === 'Quantidade') {
      return '1';
    } else if (tipo === 'Peso' || tipo === 'Volume') {
      return '0.001';
    }
    return '0.0001'; // Padrão para outros casos
  }

  formatQuantidade(quantidade: number, insumoId: number | null): string {
    const tipo = this.getInsumoUnidadeTipo(insumoId);
    if (tipo === 'Quantidade') {
      return Math.round(quantidade).toString();
    }
    // Para Peso e Volume, mostrar até 3 casas decimais
    return quantidade.toFixed(3).replace(/\.?0+$/, '');
  }

  onInsumoChange(item: ReceitaItemFormModel, index: number) {
    // Quando o insumo muda, ajustar a quantidade se necessário
    const tipo = this.getInsumoUnidadeTipo(item.insumoId);
    if (tipo === 'Quantidade' && item.quantidade > 0) {
      // Arredondar para inteiro
      item.quantidade = Math.round(item.quantidade);
      if (item.quantidade < 1) {
        item.quantidade = 1;
      }
      this.cdr.markForCheck();
    }
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
    // Quando a quantidade muda, ajustar se for unidade de quantidade
    const tipo = this.getInsumoUnidadeTipo(item.insumoId);
    if (tipo === 'Quantidade' && item.quantidade > 0) {
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

    const validItens = this.itens().filter(item => item.insumoId !== null && item.quantidade > 0);
    if (validItens.length === 0) {
      this.toast.error('Adicione pelo menos um item à receita');
      return;
    }

    // Arredondar quantidades baseado no tipo da unidade antes de enviar
    const itensRequest = validItens.map(item => {
      const tipo = this.getInsumoUnidadeTipo(item.insumoId);
      let quantidade = item.quantidade;
      
      // Se for unidade de quantidade, garantir que seja inteiro
      if (tipo === 'Quantidade') {
        quantidade = Math.round(quantidade);
        if (quantidade < 1) quantidade = 1;
      }
      
      return {
        insumoId: item.insumoId!,
        quantidade: quantidade,
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
        fatorRendimento: v.fatorRendimento || 1.0,
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
        fatorRendimento: v.fatorRendimento || 1.0,
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

