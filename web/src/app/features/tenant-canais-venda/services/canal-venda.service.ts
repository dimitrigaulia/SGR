import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface CanalVendaDto {
  id: number;
  nome: string;
  taxaPercentualPadrao?: number | null;
  isAtivo: boolean;
  usuarioCriacao?: string | null;
  usuarioAtualizacao?: string | null;
  dataCriacao: string;
  dataAtualizacao?: string | null;
}

export interface CreateCanalVendaRequest {
  nome: string;
  taxaPercentualPadrao?: number | null;
  isAtivo: boolean;
}

export interface UpdateCanalVendaRequest {
  nome: string;
  taxaPercentualPadrao?: number | null;
  isAtivo: boolean;
}

export interface Paged<T> { items: T[]; total: number }

/**
 * Service para gerenciamento de canais de venda do tenant
 */
@Injectable({ providedIn: 'root' })
export class CanalVendaService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/tenant/canais-venda`;

  /**
   * Lista canais com paginação, busca e ordenação
   */
  list(opts?: { search?: string; page?: number; pageSize?: number; sort?: string; order?: 'asc'|'desc' }): Observable<Paged<CanalVendaDto>> {
    const { search, page = 1, pageSize = 10, sort, order } = opts ?? {};
    const params: any = { page, pageSize };
    if (search) params.search = search;
    if (sort) params.sort = sort;
    if (order) params.order = order;
    return this.http.get<Paged<CanalVendaDto>>(this.base, { params });
  }

  /**
   * Busca um canal por ID
   */
  get(id: number): Observable<CanalVendaDto> {
    return this.http.get<CanalVendaDto>(`${this.base}/${id}`);
  }

  /**
   * Cria um novo canal
   */
  create(req: CreateCanalVendaRequest): Observable<CanalVendaDto> {
    return this.http.post<CanalVendaDto>(this.base, req);
  }

  /**
   * Atualiza um canal existente
   */
  update(id: number, req: UpdateCanalVendaRequest): Observable<CanalVendaDto> {
    return this.http.put<CanalVendaDto>(`${this.base}/${id}`, req);
  }

  /**
   * Exclui um canal
   */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
