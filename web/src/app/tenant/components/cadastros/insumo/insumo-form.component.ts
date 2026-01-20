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
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { InsumoService, CreateInsumoRequest, UpdateInsumoRequest, InsumoDto } from '../../../../features/tenant-insumos/services/insumo.service';
import { CategoriaInsumoService, CategoriaInsumoDto } from '../../../../features/tenant-categorias-insumo/services/categoria-insumo.service';
import { UnidadeMedidaService, UnidadeMedidaDto } from '../../../../features/tenant-unidades-medida/services/unidade-medida.service';
import { ToastService } from '../../../../core/services/toast.service';
import { UploadService } from '../../../../features/usuarios/services/upload.service';

type InsumoFormModel = Omit<InsumoDto, 'id' | 'categoriaNome' | 'unidadeCompraNome' | 'unidadeCompraSigla' | 'quantidadeAjustadaIPC' | 'custoPorUnidadeUsoAlternativo' | 'aproveitamentoPercentual' | 'custoPorUnidadeLimpa'>;

@Component({
  standalone: true,
  selector: 'app-tenant-insumo-form',
  imports: [CommonModule, FormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule, MatSelectModule, MatSlideToggleModule, MatSnackBarModule, MatCardModule, MatDialogModule, MatIconModule, MatTooltipModule],
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

  mostrarDetalhesCusto = signal(false);
  private dialog = inject(MatDialog);

  id = signal<number | null>(null);
  categorias = signal<CategoriaInsumoDto[]>([]);
  unidades = signal<UnidadeMedidaDto[]>([]);
  isEdit = computed(() => this.id() !== null);
  isView = signal<boolean>(false);
  error = signal<string>('');
  previousImageUrl: string | null = null;
  ipcEditadoManualmente = signal<boolean>(false);
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;
  @ViewChild('ipcInput') ipcInput!: ElementRef<HTMLInputElement>;

  model: InsumoFormModel = {
    nome: '',
    categoriaId: null as any,
    unidadeCompraId: null as any,
    quantidadePorEmbalagem: 1,
    unidadesPorEmbalagem: 0,
    pesoPorUnidade: 0,
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
          // Pré-selecionar "Grama" por padrão apenas em modo de criação
          if (!this.id() && !this.model.unidadeCompraId) {
            const grama = res.items.find(u => 
              u.sigla.toLowerCase() === 'gr' || 
              u.sigla.toLowerCase() === 'g' || 
              u.nome.toLowerCase().includes('grama')
            );
            if (grama) {
              this.model.unidadeCompraId = grama.id;
              this.cdr.markForCheck();
            }
          }
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
            quantidadePorEmbalagem: e.quantidadePorEmbalagem,
            unidadesPorEmbalagem: e.unidadesPorEmbalagem ?? 0,
            pesoPorUnidade: e.pesoPorUnidade ?? 0,
            custoUnitario: e.custoUnitario,
            fatorCorrecao: e.fatorCorrecao,
            ipcValor: e.ipcValor ?? 0,
            descricao: e.descricao || '',
            pathImagem: e.pathImagem || '',
            isAtivo: e.isAtivo
          };
          this.previousImageUrl = e.pathImagem ?? null;
          // Se IPC já existe e é diferente da quantidade, foi editado manualmente
          if (e.ipcValor && e.ipcValor !== e.quantidadePorEmbalagem) {
            this.ipcEditadoManualmente.set(true);
          }
          this.cdr.markForCheck();
        });
    }
  }

  private findUnidade(id: number | null | undefined): UnidadeMedidaDto | undefined {
    if (!id) return undefined;
    return this.unidades().find(u => u.id === id);
  }

  private obterConversaoBase(sigla?: string): { fator: number; siglaExibicao: string } {
    const upper = (sigla || '').toUpperCase();
    if (upper === 'KG') return { fator: 1000, siglaExibicao: 'g' };
    if (upper === 'L') return { fator: 1000, siglaExibicao: 'mL' };
    if (upper === 'GR') return { fator: 1, siglaExibicao: 'g' };
    if (upper === 'ML') return { fator: 1, siglaExibicao: 'mL' };
    return { fator: 1, siglaExibicao: sigla || '' };
  }

  get unidadeCompraSelecionada(): UnidadeMedidaDto | undefined {
    return this.findUnidade(this.model.unidadeCompraId);
  }

  get tiposUnidadeMensagem(): string {
    const compra = this.unidadeCompraSelecionada;

    if (!compra) {
      return 'Escolha primeiro a unidade de medida para ver como o sistema calculará os custos.';
    }

    return `Atenção: a Quantidade por Embalagem deve estar na mesma unidade de medida (${compra.sigla}) para que o custo fique correto.`;
  }

  get resumoCustoPorUnidadeCompra(): string {
    const unidadeCompra = this.findUnidade(this.model.unidadeCompraId);
    const quantidadePorEmbalagem = this.model.quantidadePorEmbalagem;
    // Garantir que custo seja número
    const custo = typeof this.model.custoUnitario === 'number' ? this.model.custoUnitario : (parseFloat(String(this.model.custoUnitario || 0)) || 0);
    
    if (!unidadeCompra || custo <= 0) {
      return '-';
    }

    // Mostrar custo da embalagem completa quando há quantidade por embalagem
    // Ex: "R$ 10,00 / 1000 g" ao invés de "R$ 10,00 / GR"
    if (quantidadePorEmbalagem > 0) {
      const valor = custo.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
      return `${valor} / ${quantidadePorEmbalagem.toLocaleString('pt-BR', { minimumFractionDigits: 0, maximumFractionDigits: 4 })} ${unidadeCompra.sigla}`;
    }

    // Caso contrário, mostrar custo por unidade
    const valor = custo.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
    return `${valor} / ${unidadeCompra.sigla}`;
  }

  get resumoCustoPorUnidade(): string {
    const unidadeCompra = this.findUnidade(this.model.unidadeCompraId);
    const quantidadePorEmbalagem = this.model.quantidadePorEmbalagem;
    // Garantir que custo seja numero
    const custo = typeof this.model.custoUnitario === 'number' ? this.model.custoUnitario : (parseFloat(String(this.model.custoUnitario || 0)) || 0);
    const ipcValor = this.model.ipcValor;

    if (!unidadeCompra || quantidadePorEmbalagem <= 0 || custo <= 0) {
      return '-';
    }

    const { fator, siglaExibicao } = this.obterConversaoBase(unidadeCompra.sigla);
    const quantidadeBase = quantidadePorEmbalagem * fator;
    const ipcBase = ipcValor && ipcValor > 0 ? ipcValor * fator : quantidadeBase;

    if (quantidadeBase <= 0 || ipcBase <= 0) {
      return '-';
    }

    const custoPorUnidadeBase = custo / ipcBase;
    const valor = custoPorUnidadeBase.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
    return `${valor} / ${siglaExibicao || unidadeCompra.sigla}`;
  }


  get resumoCustoPorUnidadeLimpa(): string {
    const quantidadePorEmbalagem = this.model.quantidadePorEmbalagem;
    const unidadesPorEmbalagem = this.model.unidadesPorEmbalagem || 0;
    const custo = typeof this.model.custoUnitario === 'number' ? this.model.custoUnitario : (parseFloat(String(this.model.custoUnitario || 0)) || 0);
    const ipcValor = this.model.ipcValor;
    const unidadeCompra = this.findUnidade(this.model.unidadeCompraId);

    if (unidadesPorEmbalagem <= 0 || quantidadePorEmbalagem <= 0 || custo <= 0) {
      return '-';
    }

    if (!unidadeCompra) {
      return '-';
    }

    const { fator } = this.obterConversaoBase(unidadeCompra.sigla);
    const quantidadeBase = quantidadePorEmbalagem * fator;
    const ipcBase = ipcValor && ipcValor > 0 ? ipcValor * fator : quantidadeBase;
    if (ipcBase <= 0) {
      return '-';
    }

    const custoPorUnidade = (quantidadeBase / ipcBase) * (custo / unidadesPorEmbalagem);
    const valor = custoPorUnidade.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
    return `${valor} / un`;
  }

  get exibirDetalhesCusto(): boolean {
    const unidades = this.model.unidadesPorEmbalagem || 0;
    if (unidades <= 0) {
      return true;
    }
    return this.mostrarDetalhesCusto();
  }

  toggleDetalhesCusto(): void {
    this.mostrarDetalhesCusto.set(!this.mostrarDetalhesCusto());
    this.cdr.markForCheck();
  }


  /**
   * Limpa a entrada removendo caracteres inválidos, mantendo apenas números, ponto e vírgula
   */
  private limparEntradaNumerica(valor: string): string {
    // Remover todos os caracteres que não são números, ponto ou vírgula
    return valor.replace(/[^0-9.,]/g, '');
  }

  /**
   * Normaliza número aceitando tanto ponto quanto vírgula como separador decimal
   * Remove pontos de milhar e converte vírgula para ponto
   */
  private normalizarNumero(valor: string | number | null | undefined): number | null {
    if (valor === null || valor === undefined || valor === '') {
      return null;
    }
    
    // Se já é número, retornar
    if (typeof valor === 'number') {
      return valor;
    }
    
    // Limpar entrada removendo caracteres inválidos
    let str = this.limparEntradaNumerica(String(valor).trim());
    
    if (str === '') {
      return null;
    }
    
    // Se tem vírgula, assumir que é separador decimal brasileiro
    if (str.includes(',')) {
      // Remover pontos (milhares) e substituir vírgula por ponto
      str = str.replace(/\./g, '').replace(',', '.');
    }
    // Se só tem pontos, verificar se é formato americano (1.5) ou brasileiro (1.000)
    else if (str.includes('.')) {
      // Se tem mais de um ponto, assumir que são milhares (1.000.000)
      const partes = str.split('.');
      if (partes.length > 2) {
        // Formato brasileiro com milhares: remover pontos
        str = str.replace(/\./g, '');
      }
      // Caso contrário, manter o ponto como decimal (formato americano)
    }
    
    // Tentar converter para número
    const num = parseFloat(str);
    
    return isNaN(num) ? null : num;
  }

  onQuantidadePorEmbalagemInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    // Limpar entrada removendo caracteres inválidos
    const valorLimpo = this.limparEntradaNumerica(input.value);
    
    // Atualizar o valor do input com a versão limpa
    if (input.value !== valorLimpo) {
      input.value = valorLimpo;
    }
    
    const valorNormalizado = this.normalizarNumero(valorLimpo);
    
    if (valorNormalizado !== null && valorNormalizado >= 0) {
      this.model.quantidadePorEmbalagem = valorNormalizado;
      this.onQuantidadePorEmbalagemChange();
      this.cdr.markForCheck();
    } else if (valorLimpo === '') {
      // Permitir campo vazio durante digitação
      this.model.quantidadePorEmbalagem = 0;
      this.cdr.markForCheck();
    }
  }

  onQuantidadePorEmbalagemChange(): void {
    // Preencher IPC automaticamente apenas se o usuário ainda não tiver mexido manualmente
    if (!this.ipcEditadoManualmente() && this.model.quantidadePorEmbalagem > 0) {
      this.model.ipcValor = this.model.quantidadePorEmbalagem;
      // Atualizar input do IPC diretamente para garantir que o valor seja exibido
      if (this.ipcInput?.nativeElement) {
        this.ipcInput.nativeElement.value = String(this.model.quantidadePorEmbalagem);
      }
      // Usar detectChanges() para garantir atualização imediata com OnPush
      this.cdr.detectChanges();
    }
  }

  onIPCInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    // Limpar entrada removendo caracteres inválidos
    const valorLimpo = this.limparEntradaNumerica(input.value);
    
    // Atualizar o valor do input com a versão limpa
    if (input.value !== valorLimpo) {
      input.value = valorLimpo;
    }
    
    const valorNormalizado = this.normalizarNumero(valorLimpo);
    
    if (valorNormalizado !== null && valorNormalizado >= 0) {
      this.model.ipcValor = valorNormalizado;
      this.onIPCChange();
      this.cdr.markForCheck();
    } else if (valorLimpo === '') {
      // Permitir campo vazio durante digitação
      this.model.ipcValor = 0;
      this.cdr.markForCheck();
    }
  }

  onIPCChange(): void {
    // Marcar que o usuario editou o IPC manualmente
    this.ipcEditadoManualmente.set(true);
    
    // Validar que IPC nao ultrapasse a quantidade por embalagem
    if (this.model.ipcValor && this.model.quantidadePorEmbalagem > 0) {
      if (this.model.ipcValor > this.model.quantidadePorEmbalagem) {
        this.model.ipcValor = this.model.quantidadePorEmbalagem;
        this.toast.error('IPC nao pode ser maior que Quantidade por Embalagem');
        this.cdr.markForCheck();
      }
    }

    this.atualizarPesoPorUnidade(false);
  }

  private atualizarPesoPorUnidade(force: boolean): void {
    const unidades = this.model.unidadesPorEmbalagem || 0;
    const ipcValor = this.model.ipcValor || 0;
    if (unidades <= 0 || ipcValor <= 0) {
      return;
    }

    if (!force && this.model.pesoPorUnidade && this.model.pesoPorUnidade > 0) {
      return;
    }

    const unidadeCompra = this.findUnidade(this.model.unidadeCompraId);
    const { fator } = this.obterConversaoBase(unidadeCompra?.sigla);
    const ipcBase = ipcValor * fator;

    if (ipcBase <= 0) {
      return;
    }

    const calculado = ipcBase / unidades;
    if (calculado <= 0) {
      return;
    }

    this.model.pesoPorUnidade = Math.round(calculado * 100) / 100;
    this.cdr.markForCheck();
  }

  onUnidadesPorEmbalagemInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const valorLimpo = this.limparEntradaNumerica(input.value);

    if (input.value !== valorLimpo) {
      input.value = valorLimpo;
    }

    const valorNormalizado = this.normalizarNumero(valorLimpo);

    if (valorNormalizado !== null && valorNormalizado >= 0) {
      this.model.unidadesPorEmbalagem = valorNormalizado;
      this.atualizarPesoPorUnidade(false);
      this.cdr.markForCheck();
    } else if (valorLimpo === '') {
      this.model.unidadesPorEmbalagem = 0;
      this.atualizarPesoPorUnidade(false);
      this.cdr.markForCheck();
    }
  }

  onPesoPorUnidadeInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const valorLimpo = this.limparEntradaNumerica(input.value);

    if (input.value !== valorLimpo) {
      input.value = valorLimpo;
    }

    const valorNormalizado = this.normalizarNumero(valorLimpo);

    if (valorNormalizado !== null && valorNormalizado >= 0) {
      this.model.pesoPorUnidade = valorNormalizado;
      this.cdr.markForCheck();
    } else if (valorLimpo === '') {
      this.model.pesoPorUnidade = 0;
      this.cdr.markForCheck();
    }
  }

  calcularPesoPorUnidade(): void {
    this.atualizarPesoPorUnidade(true);
  }



  onCustoInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    // Limpar entrada removendo caracteres inválidos
    const valorLimpo = this.limparEntradaNumerica(input.value);
    
    // Atualizar o valor do input com a versão limpa
    if (input.value !== valorLimpo) {
      input.value = valorLimpo;
    }
    
    const valorNormalizado = this.normalizarNumero(valorLimpo);
    
    if (valorNormalizado !== null && valorNormalizado >= 0) {
      // Garantir que seja número, não string
      this.model.custoUnitario = Number(valorNormalizado);
      this.cdr.markForCheck();
    } else if (valorLimpo === '') {
      // Permitir campo vazio durante digitação
      this.model.custoUnitario = 0;
      this.cdr.markForCheck();
    }
  }

  resetarIPC(): void {
    if (this.model.quantidadePorEmbalagem > 0) {
      this.model.ipcValor = this.model.quantidadePorEmbalagem;
      this.ipcEditadoManualmente.set(false);
      this.cdr.markForCheck();
    }
  }

  get aproveitamentoPercentual(): string {
    const quantidadePorEmbalagem = this.model.quantidadePorEmbalagem;
    const ipcValor = this.model.ipcValor;

    if (!quantidadePorEmbalagem || quantidadePorEmbalagem <= 0) {
      return '-';
    }

    if (!ipcValor || ipcValor <= 0) {
      return '100%';
    }

    const percentual = (ipcValor / quantidadePorEmbalagem) * 100;
    return `${percentual.toFixed(1)}%`;
  }

  get exemploIPC(): string {
    const unidadeCompra = this.findUnidade(this.model.unidadeCompraId);
    const quantidadePorEmbalagem = this.model.quantidadePorEmbalagem;
    const ipcValor = this.model.ipcValor || quantidadePorEmbalagem;
    const custo = this.model.custoUnitario || 0;
    const sigla = unidadeCompra?.sigla || 'g';

    if (quantidadePorEmbalagem <= 0 || custo <= 0) {
      return '';
    }

    const qtdFormatada = quantidadePorEmbalagem.toLocaleString('pt-BR', { minimumFractionDigits: 0, maximumFractionDigits: 4 });
    const ipcFormatado = ipcValor.toLocaleString('pt-BR', { minimumFractionDigits: 0, maximumFractionDigits: 4 });
    const custoPorUnidade = ipcValor > 0 ? custo / ipcValor : custo / quantidadePorEmbalagem;
    const custoFormatado = custoPorUnidade.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL', minimumFractionDigits: 2, maximumFractionDigits: 4 });
    
    return `Compro ${qtdFormatada} ${sigla}, aproveito ${ipcFormatado} ${sigla} → custo por ${sigla} usado = ${custo.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })} / ${ipcFormatado} = ${custoFormatado}`;
  }

  get aproveitamentoPercentualNumero(): number {
    const quantidadePorEmbalagem = this.model.quantidadePorEmbalagem;
    const ipcValor = this.model.ipcValor;

    if (!quantidadePorEmbalagem || quantidadePorEmbalagem <= 0) {
      return 100;
    }

    if (!ipcValor || ipcValor <= 0) {
      return 100;
    }

    return (ipcValor / quantidadePorEmbalagem) * 100;
  }

  save() {
    this.error.set('');
    if (this.isView()) return;
    const v = this.model;
    // Validação simples
    if (!v.nome || !v.categoriaId || !v.unidadeCompraId || !v.quantidadePorEmbalagem || v.quantidadePorEmbalagem <= 0) {
      this.toast.error('Preencha os campos obrigatórios corretamente');
      return;
    }
    
    if (!v.custoUnitario || v.custoUnitario < 0) {
      this.toast.error('Custo da embalagem é obrigatório e deve ser maior ou igual a zero');
      return;
    }

    // Validações do IPC
    if (v.ipcValor) {
      if (v.ipcValor <= 0) {
        this.toast.error('IPC deve ser maior que zero');
        return;
      }
      if (v.ipcValor > v.quantidadePorEmbalagem) {
        this.toast.error('IPC não pode ser maior que Quantidade por Embalagem');
        return;
      }
      
      // Verificar aproveitamento muito baixo (< 30%)
      const aproveitamento = this.aproveitamentoPercentualNumero;
      if (aproveitamento < 30) {
        const confirmar = confirm(`Aproveitamento muito baixo (${aproveitamento.toFixed(1)}%). Confirma?`);
        if (!confirmar) {
          return;
        }
      }
    }

    if (v.unidadesPorEmbalagem && v.unidadesPorEmbalagem <= 0) {
      this.toast.error('Unidades por embalagem deve ser maior que zero');
      return;
    }

    if (v.pesoPorUnidade && v.pesoPorUnidade <= 0) {
      this.toast.error('Peso por unidade deve ser maior que zero');
      return;
    }

    if (!this.isEdit()) {
      const req: CreateInsumoRequest = {
        nome: v.nome,
        categoriaId: v.categoriaId!,
        unidadeCompraId: v.unidadeCompraId!,
        quantidadePorEmbalagem: v.quantidadePorEmbalagem,
        unidadesPorEmbalagem: v.unidadesPorEmbalagem && v.unidadesPorEmbalagem > 0 ? v.unidadesPorEmbalagem : undefined,
        pesoPorUnidade: v.pesoPorUnidade && v.pesoPorUnidade > 0 ? v.pesoPorUnidade : undefined,
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
        quantidadePorEmbalagem: v.quantidadePorEmbalagem,
        unidadesPorEmbalagem: v.unidadesPorEmbalagem && v.unidadesPorEmbalagem > 0 ? v.unidadesPorEmbalagem : undefined,
        pesoPorUnidade: v.pesoPorUnidade && v.pesoPorUnidade > 0 ? v.pesoPorUnidade : undefined,
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
