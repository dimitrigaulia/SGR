import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface FichaTecnicaCanalDto {
  id: number;
  fichaTecnicaId: number;
  canal: string;
  nomeExibicao?: string | null;
  precoVenda: number;
  taxaPercentual?: number | null;
  comissaoPercentual?: number | null;
  margemCalculadaPercentual?: number | null;
  observacoes?: string | null;
  isAtivo: boolean;
}

export interface FichaTecnicaDto {
  id: number;
  receitaId: number;
  receitaNome: string;
  nome: string;
  codigo?: string | null;
  descricaoComercial?: string | null;
  rendimentoFinal?: number | null;
  indiceContabil?: number | null;
  precoSugeridoVenda?: number | null;
  margemAlvoPercentual?: number | null;
  custoTecnicoTotal: number;
  custoTecnicoPorPorcao: number;
  isAtivo: boolean;
  usuarioCriacao?: string | null;
  usuarioAtualizacao?: string | null;
  dataCriacao: string;
  dataAtualizacao?: string | null;
  canais: FichaTecnicaCanalDto[];
}

export interface CreateFichaTecnicaCanalRequest {
  canal: string;
  nomeExibicao?: string | null;
  precoVenda: number;
  taxaPercentual?: number | null;
  comissaoPercentual?: number | null;
  observacoes?: string | null;
  isAtivo: boolean;
}

export interface UpdateFichaTecnicaCanalRequest extends CreateFichaTecnicaCanalRequest {
  id?: number | null;
}

export interface CreateFichaTecnicaRequest {
  receitaId: number;
  nome: string;
  codigo?: string | null;
  descricaoComercial?: string | null;
  rendimentoFinal?: number | null;
  indiceContabil?: number | null;
  margemAlvoPercentual?: number | null;
  isAtivo: boolean;
  canais: CreateFichaTecnicaCanalRequest[];
}

export interface UpdateFichaTecnicaRequest {
  receitaId: number;
  nome: string;
  codigo?: string | null;
  descricaoComercial?: string | null;
  rendimentoFinal?: number | null;
  indiceContabil?: number | null;
  margemAlvoPercentual?: number | null;
  isAtivo: boolean;
  canais: UpdateFichaTecnicaCanalRequest[];
}

export interface Paged<T> { items: T[]; total: number }

@Injectable({ providedIn: 'root' })
export class FichaTecnicaService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/tenant/fichas-tecnicas`;

  list(opts?: { search?: string; page?: number; pageSize?: number; sort?: string; order?: 'asc' | 'desc' }): Observable<Paged<FichaTecnicaDto>> {
    const { search, page = 1, pageSize = 10, sort, order } = opts ?? {};
    const params: any = { page, pageSize };
    if (search) params.search = search;
    if (sort) params.sort = sort;
    if (order) params.order = order;
    return this.http.get<Paged<FichaTecnicaDto>>(this.base, { params });
  }

  get(id: number): Observable<FichaTecnicaDto> {
    return this.http.get<FichaTecnicaDto>(`${this.base}/${id}`);
  }

  create(req: CreateFichaTecnicaRequest): Observable<FichaTecnicaDto> {
    return this.http.post<FichaTecnicaDto>(this.base, req);
  }

  update(id: number, req: UpdateFichaTecnicaRequest): Observable<FichaTecnicaDto> {
    return this.http.put<FichaTecnicaDto>(`${this.base}/${id}`, req);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
