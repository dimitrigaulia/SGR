import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface TenantPerfilDto {
  id: number;
  nome: string;
  isAtivo: boolean;
}

export interface CreateTenantPerfilRequest {
  nome: string;
  isAtivo: boolean;
}

export interface UpdateTenantPerfilRequest {
  nome: string;
  isAtivo: boolean;
}

export interface Paged<T> { items: T[]; total: number }

/**
 * Service para gerenciamento de perfis do tenant
 */
@Injectable({ providedIn: 'root' })
export class TenantPerfilService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/tenant/perfis`;

  /**
   * Lista perfis com paginação, busca e ordenação
   */
  list(opts?: { search?: string; page?: number; pageSize?: number; sort?: string; order?: 'asc'|'desc' }): Observable<Paged<TenantPerfilDto>> {
    const { search, page = 1, pageSize = 10, sort, order } = opts ?? {};
    const params: any = { page, pageSize };
    if (search) params.search = search;
    if (sort) params.sort = sort;
    if (order) params.order = order;
    return this.http.get<Paged<TenantPerfilDto>>(this.base, { params });
  }

  /**
   * Busca um perfil por ID
   */
  get(id: number): Observable<TenantPerfilDto> {
    return this.http.get<TenantPerfilDto>(`${this.base}/${id}`);
  }

  /**
   * Cria um novo perfil
   */
  create(req: CreateTenantPerfilRequest): Observable<TenantPerfilDto> {
    return this.http.post<TenantPerfilDto>(this.base, req);
  }

  /**
   * Atualiza um perfil existente
   */
  update(id: number, req: UpdateTenantPerfilRequest): Observable<TenantPerfilDto> {
    return this.http.put<TenantPerfilDto>(`${this.base}/${id}`, req);
  }

  /**
   * Exclui um perfil
   */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}

