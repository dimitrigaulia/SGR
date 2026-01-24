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
import { MatExpansionModule } from '@angular/material/expansion';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';

type InsumoFormModel = Omit<InsumoDto, 'id' | 'categoriaNome' | 'unidadeCompraNome' | 'unidadeCompraSigla' | 'quantidadeAjustadaIPC' | 'custoPorUnidadeUsoAlternativo' | 'aproveitamentoPercentual' | 'custoPorUnidadeLimpa'>;

@Component({
  standalone: true,
  selector: 'app-tenant-insumo-form',
  imports: [CommonModule, FormsModule, RouterLink, MatFormFieldModule,  MatInputModule, MatButtonModule, MatSelectModule, MatSlideToggleModule, MatSnackBarModule, MatCardModule, MatDialogModule, MatIconModule, MatTooltipModule, MatExpansionModule],
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

  // Campos string para binding com ngModel (validações do form)
  quantidadePorEmbalagemStr = '';
  ipcValorStr = '';
  custoUnitarioStr = '';
  unidadesPorEmbalagemStr = '';
  pesoPorUnidadeStr = '';

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
          // Inicializar campos string para ngModel
          this.syncModelToStrings();
          this.cdr.markForCheck();
        });
    } else {
      // Inicializar campos string para criação
      this.syncModelToStrings();
    }
  }

  private syncModelToStrings(): void {
    this.quantidadePorEmbalagemStr = this.model.quantidadePorEmbalagem > 0 ? String(this.model.quantidadePorEmbalagem) : '';
    this.ipcValorStr = (this.model.ipcValor || 0) > 0 ? String(this.model.ipcValor) : '';
    this.custoUnitarioStr = this.model.custoUnitario > 0 ? String(this.model.custoUnitario) : '';
    this.unidadesPorEmbalagemStr = (this.model.unidadesPorEmbalagem || 0) > 0 ? String(this.model.unidadesPorEmbalagem) : '';
    this.pesoPorUnidadeStr = (this.model.pesoPorUnidade || 0) > 0 ? String(this.model.pesoPorUnidade) : '';
  }

  /**
   * Valida keydown para permitir apenas números, vírgula, ponto e teclas de controle
   */
  onNumericKeyDown(event: KeyboardEvent, allowDecimal: boolean = true): void {
    const key = event.key;
    
    // Permitir teclas de controle
    if (
      key === 'Backspace' ||
      key === 'Delete' ||
      key === 'Tab' ||
      key === 'Escape' ||
      key === 'Enter' ||
      key === 'ArrowLeft' ||
      key === 'ArrowRight' ||
      key === 'ArrowUp' ||
      key === 'ArrowDown' ||
      key === 'Home' ||
      key === 'End' ||
      (event.ctrlKey && (key === 'a' || key === 'c' || key === 'v' || key === 'x' || key === 'z'))
    ) {
      return;
    }
    
    // Permitir números
    if (key >= '0' && key <= '9') {
      return;
    }
    
    // Permitir vírgula e ponto se decimal for permitido
    if (allowDecimal && (key === ',' || key === '.')) {
      return;
    }
    
    // Bloquear qualquer outra tecla
    event.preventDefault();
  }

  /**
   * Valida keydown para permitir apenas números inteiros
   */
  onIntegerKeyDown(event: KeyboardEvent): void {
    this.onNumericKeyDown(event, false);
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

  obterUnidadeBase(): string {
    const unidadeCompra = this.unidadeCompraSelecionada;
    if (!unidadeCompra) return 'g/mL';
    const { siglaExibicao } = this.obterConversaoBase(unidadeCompra.sigla);
    return siglaExibicao;
  }

  obterLabelPesoPorUnidade(): string {
    const unidadeBase = this.obterUnidadeBase();
    if (unidadeBase === 'mL') {
      return `Volume limpo por unidade (${unidadeBase})`;
    }
    return `Peso limpo por unidade (${unidadeBase})`;
  }

  getCalcularTooltip(): string {
    const unidadeCompra = this.unidadeCompraSelecionada;
    if (!unidadeCompra) {
      return 'Selecione uma unidade de compra primeiro';
    }
    if (!this.model.quantidadePorEmbalagem || !this.model.unidadesPorEmbalagem) {
      return 'Preencha quantidade por embalagem e unidades por embalagem primeiro';
    }
    const { fator, siglaExibicao } = this.obterConversaoBase(unidadeCompra.sigla);
    const base = (this.model.quantidadePorEmbalagem * fator) / this.model.unidadesPorEmbalagem;
    return `Calcula automaticamente: Quantidade ÷ Unidades. Ex: ${this.model.quantidadePorEmbalagem} ${unidadeCompra.sigla} ÷ ${this.model.unidadesPorEmbalagem} un = ${base.toFixed(2)} ${siglaExibicao}/un`;
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

  get quantidadeUnidadesTexto(): string {
    const unidades = this.model.unidadesPorEmbalagem || 0;
    if (unidades <= 0) {
      return '';
    }
    return `Contém ${unidades} unidade${unidades !== 1 ? 's' : ''}`;
  }

  get resumoCustoPorUnidadeComprada(): string {
    const unidades = this.model.unidadesPorEmbalagem || 0;
    const custo = typeof this.model.custoUnitario === 'number' 
      ? this.model.custoUnitario 
      : (parseFloat(String(this.model.custoUnitario || 0)) || 0);
    
    if (unidades <= 0 || custo <= 0) {
      return '-';
    }
    
    const custoPorUnidade = custo / unidades;
    const valor = custoPorUnidade.toLocaleString('pt-BR', { 
      style: 'currency', 
      currency: 'BRL',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2
    });
    return `${valor} / un`;
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

  onQuantidadePorEmbalagemChange(valor: string): void {
    const valorNormalizado = this.normalizarNumero(valor);
    
    if (valorNormalizado !== null && valorNormalizado >= 0) {
      this.model.quantidadePorEmbalagem = valorNormalizado;
      this.syncIPCAutomatico();
      this.cdr.markForCheck();
    } else {
      this.model.quantidadePorEmbalagem = 0;
      this.cdr.markForCheck();
    }
  }

  private syncIPCAutomatico(): void {
    // Preencher IPC automaticamente apenas se o usuário ainda não tiver mexido manualmente
    if (!this.ipcEditadoManualmente() && this.model.quantidadePorEmbalagem > 0) {
      this.model.ipcValor = this.model.quantidadePorEmbalagem;
      this.ipcValorStr = String(this.model.quantidadePorEmbalagem);
      this.cdr.markForCheck();
    }
  }

  onIPCValorChange(valor: string): void {
    const valorNormalizado = this.normalizarNumero(valor);
    
    if (valorNormalizado !== null && valorNormalizado >= 0) {
      this.model.ipcValor = valorNormalizado;
      this.onIPCEditado();
      this.cdr.markForCheck();
    } else {
      this.model.ipcValor = 0;
      this.cdr.markForCheck();
    }
  }

  private onIPCEditado(): void {
    // Marcar que o usuario editou o IPC manualmente
    this.ipcEditadoManualmente.set(true);
    
    // Validar que IPC nao ultrapasse a quantidade por embalagem
    if (this.model.ipcValor && this.model.quantidadePorEmbalagem > 0) {
      if (this.model.ipcValor > this.model.quantidadePorEmbalagem) {
        this.model.ipcValor = this.model.quantidadePorEmbalagem;
        this.toast.error('Quantidade limpa não pode ser maior que a quantidade por embalagem');
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

  onUnidadesPorEmbalagemChange(valor: string): void {
    // Aceitar apenas inteiros
    const valorLimpo = valor.replace(/[^0-9]/g, '');
    const n = parseInt(valorLimpo, 10);
    
    if (Number.isFinite(n) && n >= 0) {
      this.model.unidadesPorEmbalagem = n;
      this.atualizarPesoPorUnidade(false);
      this.cdr.markForCheck();
    } else {
      this.model.unidadesPorEmbalagem = 0;
      this.atualizarPesoPorUnidade(false);
      this.cdr.markForCheck();
    }
  }

  onPesoPorUnidadeChange(valor: string): void {
    const valorNormalizado = this.normalizarNumero(valor);

    if (valorNormalizado !== null && valorNormalizado >= 0) {
      this.model.pesoPorUnidade = valorNormalizado;
      this.cdr.markForCheck();
    } else {
      this.model.pesoPorUnidade = 0;
      this.cdr.markForCheck();
    }
  }

  calcularPesoPorUnidade(): void {
    this.atualizarPesoPorUnidade(true);
    this.pesoPorUnidadeStr = (this.model.pesoPorUnidade || 0) > 0 ? String(this.model.pesoPorUnidade) : '';
    this.cdr.markForCheck();
  }



  onCustoUnitarioChange(valor: string): void {
    const valorNormalizado = this.normalizarNumero(valor);
    
    if (valorNormalizado !== null && valorNormalizado >= 0) {
      this.model.custoUnitario = Number(valorNormalizado);
      this.cdr.markForCheck();
    } else {
      this.model.custoUnitario = 0;
      this.cdr.markForCheck();
    }
  }

  resetarIPC(): void {
    if (this.model.quantidadePorEmbalagem > 0) {
      this.model.ipcValor = this.model.quantidadePorEmbalagem;
      this.ipcValorStr = String(this.model.quantidadePorEmbalagem);
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
      return '100.0%';
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
    
    if (!v.custoUnitario || v.custoUnitario <= 0) {
      this.toast.error('Custo da embalagem é obrigatório e deve ser maior que zero');
      return;
    }

    // Validações do IPC
    if (v.ipcValor) {
      if (v.ipcValor <= 0) {
        this.toast.error('Quantidade limpa deve ser maior que zero');
        return;
      }
      if (v.ipcValor > v.quantidadePorEmbalagem) {
        this.toast.error('Quantidade limpa não pode ser maior que a quantidade por embalagem');
        return;
      }
      
      // Verificar aproveitamento muito baixo (< 30%)
      const aproveitamento = this.aproveitamentoPercentualNumero;
      if (aproveitamento < 30) {
        this.confirmarAproveitamentoBaixo(aproveitamento);
        return;
      }
    }

    this.salvarInsumo();
  }

  private confirmarAproveitamentoBaixo(aproveitamento: number): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: 'Aproveitamento muito baixo',
        message: `O aproveitamento está em ${aproveitamento.toFixed(1)}%. Isso pode indicar um erro de preenchimento. Deseja continuar mesmo assim?`,
        confirmText: 'Sim, continuar',
        cancelText: 'Revisar'
      }
    });

    dialogRef.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(result => {
        if (result) {
          this.salvarInsumo();
        }
      });
  }

  private salvarInsumo(): void {
    const v = this.model;

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
