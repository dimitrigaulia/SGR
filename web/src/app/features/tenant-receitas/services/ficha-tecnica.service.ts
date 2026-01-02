import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface FichaTecnicaCanalDto {
  id: number;
  fichaTecnicaId: number;
  canalVendaId?: number | null;
  canal: string;
  nomeExibicao?: string | null;
  precoVenda: number;
  taxaPercentual?: number | null;
  comissaoPercentual?: number | null;
  multiplicador?: number | null;
  margemCalculadaPercentual?: number | null;
  observacoes?: string | null;
  isAtivo: boolean;
}

export interface FichaTecnicaItemDto {
  id: number;
  fichaTecnicaId: number;
  tipoItem: string;
  receitaId?: number | null;
  receitaNome?: string | null;
  insumoId?: number | null;
  insumoNome?: string | null;
  quantidade: number;
  unidadeMedidaId: number;
  unidadeMedidaNome?: string | null;
  unidadeMedidaSigla?: string | null;
  exibirComoQB: boolean;
  ordem: number;
  observacoes?: string | null;
  custoItem: number;
}

export interface FichaTecnicaDto {
  id: number;
  categoriaId: number;
  categoriaNome?: string | null;
  receitaPrincipalId?: number | null;
  receitaPrincipalNome?: string | null;
  nome: string;
  codigo?: string | null;
  descricaoComercial?: string | null;
  custoTotal: number;
  custoPorUnidade: number;
  rendimentoFinal?: number | null;
  indiceContabil?: number | null;
  precoSugeridoVenda?: number | null;
  icOperador?: string | null;
  icValor?: number | null;
  ipcValor?: number | null;
  margemAlvoPercentual?: number | null;
  porcaoVendaQuantidade?: number | null;
  porcaoVendaUnidadeMedidaId?: number | null;
  porcaoVendaUnidadeMedidaNome?: string | null;
  porcaoVendaUnidadeMedidaSigla?: string | null;
  rendimentoPorcoes?: string | null;
  tempoPreparo?: number | null;
  pesoTotalBase?: number | null;
  custoKgL?: number | null;
  custoPorPorcaoVenda?: number | null;
  precoMesaSugerido?: number | null;
  isAtivo: boolean;
  usuarioCriacao?: string | null;
  usuarioAtualizacao?: string | null;
  dataCriacao: string;
  dataAtualizacao?: string | null;
  itens: FichaTecnicaItemDto[];
  canais: FichaTecnicaCanalDto[];
}

export interface CreateFichaTecnicaCanalRequest {
  canal: string;
  nomeExibicao?: string | null;
  precoVenda: number;
  taxaPercentual?: number | null;
  comissaoPercentual?: number | null;
  multiplicador?: number | null;
  observacoes?: string | null;
  isAtivo: boolean;
}

export interface UpdateFichaTecnicaCanalRequest extends CreateFichaTecnicaCanalRequest {
  id?: number | null;
}

export interface CreateFichaTecnicaItemRequest {
  tipoItem: string;
  receitaId?: number | null;
  insumoId?: number | null;
  quantidade: number;
  unidadeMedidaId: number;
  exibirComoQB?: boolean;
  ordem: number;
  observacoes?: string | null;
}

export interface UpdateFichaTecnicaItemRequest {
  id?: number | null;
  tipoItem: string;
  receitaId?: number | null;
  insumoId?: number | null;
  quantidade: number;
  unidadeMedidaId: number;
  exibirComoQB?: boolean;
  ordem: number;
  observacoes?: string | null;
}

export interface CreateFichaTecnicaRequest {
  categoriaId: number;
  receitaPrincipalId?: number | null;
  nome: string;
  codigo?: string | null;
  descricaoComercial?: string | null;
  indiceContabil?: number | null;
  icOperador?: string | null;
  icValor?: number | null;
  ipcValor?: number | null;
  margemAlvoPercentual?: number | null;
  porcaoVendaQuantidade?: number | null;
  porcaoVendaUnidadeMedidaId?: number | null;
  rendimentoPorcoes?: string | null;
  tempoPreparo?: number | null;
  isAtivo: boolean;
  itens: CreateFichaTecnicaItemRequest[];
  canais: CreateFichaTecnicaCanalRequest[];
}

export interface UpdateFichaTecnicaRequest {
  categoriaId: number;
  receitaPrincipalId?: number | null;
  nome: string;
  codigo?: string | null;
  descricaoComercial?: string | null;
  indiceContabil?: number | null;
  icOperador?: string | null;
  icValor?: number | null;
  ipcValor?: number | null;
  margemAlvoPercentual?: number | null;
  porcaoVendaQuantidade?: number | null;
  porcaoVendaUnidadeMedidaId?: number | null;
  rendimentoPorcoes?: string | null;
  tempoPreparo?: number | null;
  isAtivo: boolean;
  itens: UpdateFichaTecnicaItemRequest[];
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
