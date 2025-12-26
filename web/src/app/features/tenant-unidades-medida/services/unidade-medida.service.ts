import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface UnidadeMedidaDto {
  id: number;
  nome: string;
  sigla: string;
  isAtivo: boolean;
  usuarioCriacao?: string | null;
  usuarioAtualizacao?: string | null;
  dataCriacao: string;
  dataAtualizacao?: string | null;
}

export interface CreateUnidadeMedidaRequest {
  nome: string;
  sigla: string;
  isAtivo: boolean;
}

export interface UpdateUnidadeMedidaRequest {
  nome: string;
  sigla: string;
  isAtivo: boolean;
}

export interface Paged<T> { items: T[]; total: number }

/**
 * Service para gerenciamento de unidades de medida do tenant
 */
@Injectable({ providedIn: 'root' })
export class UnidadeMedidaService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/tenant/unidades-medida`;

  /**
   * Lista unidades com paginação, busca e ordenação
   */
  list(opts?: { search?: string; page?: number; pageSize?: number; sort?: string; order?: 'asc'|'desc' }): Observable<Paged<UnidadeMedidaDto>> {
    const { search, page = 1, pageSize = 10, sort, order } = opts ?? {};
    const params: any = { page, pageSize };
    if (search) params.search = search;
    if (sort) params.sort = sort;
    if (order) params.order = order;
    return this.http.get<Paged<UnidadeMedidaDto>>(this.base, { params });
  }

  /**
   * Busca uma unidade por ID
   */
  get(id: number): Observable<UnidadeMedidaDto> {
    return this.http.get<UnidadeMedidaDto>(`${this.base}/${id}`);
  }

  /**
   * Cria uma nova unidade
   */
  create(req: CreateUnidadeMedidaRequest): Observable<UnidadeMedidaDto> {
    return this.http.post<UnidadeMedidaDto>(this.base, req);
  }

  /**
   * Atualiza uma unidade existente
   */
  update(id: number, req: UpdateUnidadeMedidaRequest): Observable<UnidadeMedidaDto> {
    return this.http.put<UnidadeMedidaDto>(`${this.base}/${id}`, req);
  }

  /**
   * Exclui uma unidade
   */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}

