import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface UsuarioDto {
  id: number;
  perfilId: number;
  isAtivo: boolean;
  nomeCompleto: string;
  email: string;
  pathImagem?: string | null;
}

export interface CreateUsuarioRequest {
  perfilId: number;
  isAtivo: boolean;
  nomeCompleto: string;
  email: string;
  senha: string;
  pathImagem?: string | null;
}

export interface UpdateUsuarioRequest {
  perfilId: number;
  isAtivo: boolean;
  nomeCompleto: string;
  email: string;
  novaSenha?: string | null;
  pathImagem?: string | null;
}

export interface Paged<T> { items: T[]; total: number }

/**
 * Service para gerenciamento de usuários
 */
@Injectable({ providedIn: 'root' })
export class UsuarioService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/backoffice/usuarios`;

  /**
   * Lista usuários com paginação, busca e ordenação
   */
  list(opts?: { search?: string; page?: number; pageSize?: number; sort?: string; order?: 'asc'|'desc' }): Observable<Paged<UsuarioDto>> {
    const { search, page = 1, pageSize = 10, sort, order } = opts ?? {};
    const params: any = { page, pageSize };
    if (search) params.search = search;
    if (sort) params.sort = sort;
    if (order) params.order = order;
    return this.http.get<Paged<UsuarioDto>>(this.base, { params });
  }

  /**
   * Busca um usuário por ID
   */
  get(id: number): Observable<UsuarioDto> {
    return this.http.get<UsuarioDto>(`${this.base}/${id}`);
  }

  /**
   * Cria um novo usuário
   */
  create(req: CreateUsuarioRequest): Observable<UsuarioDto> {
    return this.http.post<UsuarioDto>(this.base, req);
  }

  /**
   * Atualiza um usuário existente
   */
  update(id: number, req: UpdateUsuarioRequest): Observable<UsuarioDto> {
    return this.http.put<UsuarioDto>(`${this.base}/${id}`, req);
  }

  /**
   * Exclui um usuário
   */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  /**
   * Verifica se um email já está em uso
   */
  checkEmail(email: string, excludeId?: number): Observable<{ exists: boolean }> {
    const params: any = { email };
    if (excludeId) params.excludeId = excludeId;
    return this.http.get<{ exists: boolean }>(`${this.base}/check-email`, { params });
  }
}

