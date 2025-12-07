import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface CategoriaReceitaDto {
  id: number;
  nome: string;
  isAtivo: boolean;
}

export interface CreateCategoriaReceitaRequest {
  nome: string;
  isAtivo: boolean;
}

export interface UpdateCategoriaReceitaRequest {
  nome: string;
  isAtivo: boolean;
}

export interface Paged<T> { items: T[]; total: number }

/**
 * Service para gerenciamento de categorias de receita do tenant
 */
@Injectable({ providedIn: 'root' })
export class CategoriaReceitaService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/tenant/categorias-receita`;

  /**
   * Lista categorias com paginaÃ§Ã£o, busca e ordenaÃ§Ã£o
   */
  list(opts?: { search?: string; page?: number; pageSize?: number; sort?: string; order?: 'asc'|'desc' }): Observable<Paged<CategoriaReceitaDto>> {
    const { search, page = 1, pageSize = 10, sort, order } = opts ?? {};
    const params: any = { page, pageSize };
    if (search) params.search = search;
    if (sort) params.sort = sort;
    if (order) params.order = order;
    return this.http.get<Paged<CategoriaReceitaDto>>(this.base, { params });
  }

  /**
   * Busca uma categoria por ID
   */
  get(id: number): Observable<CategoriaReceitaDto> {
    return this.http.get<CategoriaReceitaDto>(`${this.base}/${id}`);
  }

  /**
   * Cria uma nova categoria
   */
  create(req: CreateCategoriaReceitaRequest): Observable<CategoriaReceitaDto> {
    return this.http.post<CategoriaReceitaDto>(this.base, req);
  }

  /**
   * Atualiza uma categoria existente
   */
  update(id: number, req: UpdateCategoriaReceitaRequest): Observable<CategoriaReceitaDto> {
    return this.http.put<CategoriaReceitaDto>(`${this.base}/${id}`, req);
  }

  /**
   * Exclui uma categoria
   */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}

