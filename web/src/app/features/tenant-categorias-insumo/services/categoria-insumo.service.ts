import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface CategoriaInsumoDto {
  id: number;
  nome: string;
  isAtivo: boolean;
}

export interface CreateCategoriaInsumoRequest {
  nome: string;
  isAtivo: boolean;
}

export interface UpdateCategoriaInsumoRequest {
  nome: string;
  isAtivo: boolean;
}

export interface Paged<T> { items: T[]; total: number }

/**
 * Service para gerenciamento de categorias de insumo do tenant
 */
@Injectable({ providedIn: 'root' })
export class CategoriaInsumoService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/tenant/categorias-insumo`;

  /**
   * Lista categorias com paginaÃ§Ã£o, busca e ordenaÃ§Ã£o
   */
  list(opts?: { search?: string; page?: number; pageSize?: number; sort?: string; order?: 'asc'|'desc' }): Observable<Paged<CategoriaInsumoDto>> {
    const { search, page = 1, pageSize = 10, sort, order } = opts ?? {};
    const params: any = { page, pageSize };
    if (search) params.search = search;
    if (sort) params.sort = sort;
    if (order) params.order = order;
    return this.http.get<Paged<CategoriaInsumoDto>>(this.base, { params });
  }

  /**
   * Busca uma categoria por ID
   */
  get(id: number): Observable<CategoriaInsumoDto> {
    return this.http.get<CategoriaInsumoDto>(`${this.base}/${id}`);
  }

  /**
   * Cria uma nova categoria
   */
  create(req: CreateCategoriaInsumoRequest): Observable<CategoriaInsumoDto> {
    return this.http.post<CategoriaInsumoDto>(this.base, req);
  }

  /**
   * Atualiza uma categoria existente
   */
  update(id: number, req: UpdateCategoriaInsumoRequest): Observable<CategoriaInsumoDto> {
    return this.http.put<CategoriaInsumoDto>(`${this.base}/${id}`, req);
  }

  /**
   * Exclui uma categoria
   */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}

