import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface TenantUsuarioDto {
  id: number;
  perfilId: number;
  perfilNome?: string | null;
  isAtivo: boolean;
  nomeCompleto: string;
  email: string;
  pathImagem?: string | null;
}

export interface CreateTenantUsuarioRequest {
  perfilId: number;
  isAtivo: boolean;
  nomeCompleto: string;
  email: string;
  senha: string;
  pathImagem?: string | null;
}

export interface UpdateTenantUsuarioRequest {
  perfilId: number;
  isAtivo: boolean;
  nomeCompleto: string;
  email: string;
  novaSenha?: string | null;
  pathImagem?: string | null;
}

export interface Paged<T> { items: T[]; total: number }

/**
 * Service para gerenciamento de usuÃ¡rios do tenant
 */
@Injectable({ providedIn: 'root' })
export class TenantUsuarioService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/tenant/usuarios`;

  /**
   * Lista usuários com paginação, busca e ordenação
   */
  list(opts?: { search?: string; page?: number; pageSize?: number; sort?: string; order?: 'asc'|'desc' }): Observable<Paged<TenantUsuarioDto>> {
    const { search, page = 1, pageSize = 10, sort, order } = opts ?? {};
    const params: any = { page, pageSize };
    if (search) params.search = search;
    if (sort) params.sort = sort;
    if (order) params.order = order;
    return this.http.get<Paged<TenantUsuarioDto>>(this.base, { params });
  }

  /**
   * Busca um usuÃ¡rio por ID
   */
  get(id: number): Observable<TenantUsuarioDto> {
    return this.http.get<TenantUsuarioDto>(`${this.base}/${id}`);
  }

  /**
   * Cria um novo usuÃ¡rio
   */
  create(req: CreateTenantUsuarioRequest): Observable<TenantUsuarioDto> {
    return this.http.post<TenantUsuarioDto>(this.base, req);
  }

  /**
   * Atualiza um usuÃ¡rio existente
   */
  update(id: number, req: UpdateTenantUsuarioRequest): Observable<TenantUsuarioDto> {
    return this.http.put<TenantUsuarioDto>(`${this.base}/${id}`, req);
  }

  /**
   * Exclui um usuÃ¡rio
   */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  /**
   * Verifica se um email jÃ¡ estÃ¡ em uso
   */
  checkEmail(email: string, excludeId?: number): Observable<{ exists: boolean }> {
    const params: any = { email };
    if (excludeId) params.excludeId = excludeId;
    return this.http.get<{ exists: boolean }>(`${this.base}/check-email`, { params });
  }
}

