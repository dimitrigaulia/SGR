import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface PerfilDto {
  id: number;
  nome: string;
  isAtivo: boolean;
}

export interface CreatePerfilRequest {
  nome: string;
  isAtivo: boolean;
}

export interface UpdatePerfilRequest {
  nome: string;
  isAtivo: boolean;
}

export interface Paged<T> { items: T[]; total: number }

@Injectable({ providedIn: 'root' })
export class PerfilService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/backoffice/perfis`;

  list(opts?: { search?: string; page?: number; pageSize?: number; sort?: string; order?: 'asc'|'desc' }): Observable<Paged<PerfilDto>> {
    const { search, page = 1, pageSize = 10, sort, order } = opts ?? {};
    const params: any = { page, pageSize };
    if (search) params.search = search;
    if (sort) params.sort = sort;
    if (order) params.order = order;
    return this.http.get<Paged<PerfilDto>>(this.base, { params });
  }

  get(id: number): Observable<PerfilDto> {
    return this.http.get<PerfilDto>(`${this.base}/${id}`);
  }

  create(req: CreatePerfilRequest): Observable<PerfilDto> {
    return this.http.post<PerfilDto>(this.base, req);
  }

  update(id: number, req: UpdatePerfilRequest): Observable<PerfilDto> {
    return this.http.put<PerfilDto>(`${this.base}/${id}`, req);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
