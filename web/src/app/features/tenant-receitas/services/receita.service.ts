import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface ReceitaItemDto {
  id: number;
  receitaId: number;
  insumoId: number;
  insumoNome?: string | null;
  insumoCategoriaNome?: string | null;
  unidadeUsoNome?: string | null;
  unidadeUsoSigla?: string | null;
  quantidade: number;
  quantidadeBruta: number;
  custoItem: number;
  ordem: number;
  observacoes?: string | null;
}

export interface ReceitaDto {
  id: number;
  nome: string;
  categoriaId: number;
  categoriaNome?: string | null;
  descricao?: string | null;
  instrucoesEmpratamento?: string | null;
  rendimento: number;
  pesoPorPorcao?: number | null;
  toleranciaPeso?: number | null;
  fatorRendimento: number;
  tempoPreparo?: number | null;
  versao?: string | null;
  custoTotal: number;
  custoPorPorcao: number;
  pathImagem?: string | null;
  isAtivo: boolean;
  usuarioCriacao?: string | null;
  usuarioAtualizacao?: string | null;
  dataCriacao: string;
  dataAtualizacao?: string | null;
  itens: ReceitaItemDto[];
}

export interface CreateReceitaItemRequest {
  insumoId: number;
  quantidade: number;
  ordem: number;
  observacoes?: string | null;
}

export interface UpdateReceitaItemRequest {
  insumoId: number;
  quantidade: number;
  ordem: number;
  observacoes?: string | null;
}

export interface CreateReceitaRequest {
  nome: string;
  categoriaId: number;
  descricao?: string | null;
  instrucoesEmpratamento?: string | null;
  rendimento: number;
  pesoPorPorcao?: number | null;
  toleranciaPeso?: number | null;
  fatorRendimento: number;
  tempoPreparo?: number | null;
  versao?: string | null;
  pathImagem?: string | null;
  itens: CreateReceitaItemRequest[];
  isAtivo: boolean;
}

export interface UpdateReceitaRequest {
  nome: string;
  categoriaId: number;
  descricao?: string | null;
  instrucoesEmpratamento?: string | null;
  rendimento: number;
  pesoPorPorcao?: number | null;
  toleranciaPeso?: number | null;
  fatorRendimento: number;
  tempoPreparo?: number | null;
  versao?: string | null;
  pathImagem?: string | null;
  itens: UpdateReceitaItemRequest[];
  isAtivo: boolean;
}

export interface DuplicarReceitaRequest {
  novoNome: string;
}

export interface Paged<T> { items: T[]; total: number }

/**
 * Service para gerenciamento de receitas do tenant
 */
@Injectable({ providedIn: 'root' })
export class ReceitaService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/tenant/receitas`;

  /**
   * Lista receitas com paginação, busca e ordenação
   */
  list(opts?: { search?: string; page?: number; pageSize?: number; sort?: string; order?: 'asc'|'desc' }): Observable<Paged<ReceitaDto>> {
    const { search, page = 1, pageSize = 10, sort, order } = opts ?? {};
    const params: any = { page, pageSize };
    if (search) params.search = search;
    if (sort) params.sort = sort;
    if (order) params.order = order;
    return this.http.get<Paged<ReceitaDto>>(this.base, { params });
  }

  /**
   * Busca uma receita por ID
   */
  get(id: number): Observable<ReceitaDto> {
    return this.http.get<ReceitaDto>(`${this.base}/${id}`);
  }

  /**
   * Cria uma nova receita
   */
  create(req: CreateReceitaRequest): Observable<ReceitaDto> {
    return this.http.post<ReceitaDto>(this.base, req);
  }

  /**
   * Atualiza uma receita existente
   */
  update(id: number, req: UpdateReceitaRequest): Observable<ReceitaDto> {
    return this.http.put<ReceitaDto>(`${this.base}/${id}`, req);
  }

  /**
   * Exclui uma receita
   */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  /**
   * Duplica uma receita existente
   */
  duplicar(id: number, novoNome: string): Observable<ReceitaDto> {
    return this.http.post<ReceitaDto>(`${this.base}/${id}/duplicar`, { novoNome });
  }
}

