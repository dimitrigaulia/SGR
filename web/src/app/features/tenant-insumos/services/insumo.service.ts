import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface InsumoDto {
  id: number;
  nome: string;
  categoriaId: number;
  categoriaNome?: string | null;
  unidadeCompraId: number;
  unidadeCompraNome?: string | null;
  unidadeCompraSigla?: string | null;
  unidadeUsoId: number;
  unidadeUsoNome?: string | null;
  unidadeUsoSigla?: string | null;
  unidadeUsoTipo?: string | null; // Tipo da unidade de uso (Peso, Volume, Quantidade)
  quantidadePorEmbalagem: number;
  custoUnitario: number;
  fatorCorrecao: number;
  ipcValor?: number | null;
  descricao?: string | null;
  pathImagem?: string | null;
  isAtivo: boolean;
}

export interface CreateInsumoRequest {
  nome: string;
  categoriaId: number;
  unidadeCompraId: number;
  unidadeUsoId: number;
  quantidadePorEmbalagem: number;
  custoUnitario: number;
  fatorCorrecao: number;
  ipcValor?: number | null;
  descricao?: string | null;
  pathImagem?: string | null;
  isAtivo: boolean;
}

export interface UpdateInsumoRequest {
  nome: string;
  categoriaId: number;
  unidadeCompraId: number;
  unidadeUsoId: number;
  quantidadePorEmbalagem: number;
  custoUnitario: number;
  fatorCorrecao: number;
  ipcValor?: number | null;
  descricao?: string | null;
  pathImagem?: string | null;
  isAtivo: boolean;
}

export interface Paged<T> { items: T[]; total: number }

/**
 * Service para gerenciamento de insumos do tenant
 */
@Injectable({ providedIn: 'root' })
export class InsumoService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/tenant/insumos`;

  /**
   * Lista insumos com paginaÃ§Ã£o, busca e ordenaÃ§Ã£o
   */
  list(opts?: { search?: string; page?: number; pageSize?: number; sort?: string; order?: 'asc'|'desc' }): Observable<Paged<InsumoDto>> {
    const { search, page = 1, pageSize = 10, sort, order } = opts ?? {};
    const params: any = { page, pageSize };
    if (search) params.search = search;
    if (sort) params.sort = sort;
    if (order) params.order = order;
    return this.http.get<Paged<InsumoDto>>(this.base, { params });
  }

  /**
   * Busca um insumo por ID
   */
  get(id: number): Observable<InsumoDto> {
    return this.http.get<InsumoDto>(`${this.base}/${id}`);
  }

  /**
   * Cria um novo insumo
   */
  create(req: CreateInsumoRequest): Observable<InsumoDto> {
    return this.http.post<InsumoDto>(this.base, req);
  }

  /**
   * Atualiza um insumo existente
   */
  update(id: number, req: UpdateInsumoRequest): Observable<InsumoDto> {
    return this.http.put<InsumoDto>(`${this.base}/${id}`, req);
  }

  /**
   * Exclui um insumo
   */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}

