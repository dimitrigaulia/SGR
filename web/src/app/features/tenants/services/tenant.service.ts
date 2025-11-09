import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface TenantDto {
  id: number;
  razaoSocial: string;
  nomeFantasia: string;
  tipoPessoaId: number;
  tipoPessoaNome: string;
  cpfCnpj: string;
  subdominio: string;
  nomeSchema: string;
  categoriaId: number;
  categoriaNome?: string | null;
  fatorContabil: number;
  isAtivo: boolean;
  usuarioAtualizacao?: string | null;
  dataAtualizacao?: string | null;
}

export interface CreateTenantRequest {
  razaoSocial: string;
  nomeFantasia: string;
  tipoPessoaId: number;
  cpfCnpj: string;
  subdominio: string;
  categoriaId: number;
  fatorContabil: number;
  admin: CreateAdminRequest;
}

export interface CreateAdminRequest {
  nomeCompleto: string;
  email: string;
  senha: string;
  confirmarSenha: string;
}

export interface UpdateTenantRequest {
  razaoSocial: string;
  nomeFantasia: string;
  tipoPessoaId: number;
  cpfCnpj: string;
  categoriaId: number;
  fatorContabil: number;
  isAtivo: boolean;
}

export interface CnpjDataResponse {
  cnpj?: string;
  razaoSocial?: string;
  nomeFantasia?: string;
  descricaoSituacaoCadastral?: string;
  dataInicioAtividade?: string;
  logradouro?: string;
  numero?: string;
  bairro?: string;
  municipio?: string;
  uf?: string;
  cep?: string;
  naturezaJuridica?: string;
}

export interface CategoriaTenantDto {
  id: number;
  nome: string;
  isAtivo: boolean;
}

export interface PagedResult<T> {
  items: T[];
  total: number;
}

/**
 * Service para gerenciamento de tenants
 */
@Injectable({
  providedIn: 'root'
})
export class TenantService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  /**
   * Busca todos os tenants ativos (para combobox no login)
   */
  getActiveTenants(): Observable<TenantDto[]> {
    return this.http.get<TenantDto[]>(`${this.apiUrl}/backoffice/tenants/active`);
  }

  /**
   * Busca tenant por subdomínio
   */
  getBySubdomain(subdomain: string): Observable<TenantDto> {
    return this.http.get<TenantDto>(`${this.apiUrl}/backoffice/tenants/subdomain/${subdomain}`);
  }

  /**
   * Lista todos os tenants com paginação
   */
  getAll(search?: string, page: number = 1, pageSize: number = 10, sort?: string, order?: string): Observable<PagedResult<TenantDto>> {
    const params: any = { page, pageSize };
    if (search) params.search = search;
    if (sort) params.sort = sort;
    if (order) params.order = order;
    
    return this.http.get<PagedResult<TenantDto>>(`${this.apiUrl}/backoffice/tenants`, { params });
  }

  /**
   * Busca tenant por ID
   */
  getById(id: number): Observable<TenantDto> {
    return this.http.get<TenantDto>(`${this.apiUrl}/backoffice/tenants/${id}`);
  }

  /**
   * Cria um novo tenant
   */
  create(request: CreateTenantRequest): Observable<TenantDto> {
    return this.http.post<TenantDto>(`${this.apiUrl}/backoffice/tenants`, request);
  }

  /**
   * Atualiza um tenant
   */
  update(id: number, request: UpdateTenantRequest): Observable<TenantDto> {
    return this.http.put<TenantDto>(`${this.apiUrl}/backoffice/tenants/${id}`, request);
  }

  /**
   * Exclui um tenant
   */
  delete(id: number): Observable<boolean> {
    return this.http.delete<boolean>(`${this.apiUrl}/backoffice/tenants/${id}`);
  }

  /**
   * Busca dados de uma empresa pelo CNPJ
   */
  getCnpjData(cnpj: string): Observable<CnpjDataResponse> {
    return this.http.get<CnpjDataResponse>(`${this.apiUrl}/backoffice/tenants/cnpj/${cnpj}`);
  }

  /**
   * Alterna o status ativo/inativo do tenant
   */
  toggleActive(id: number): Observable<boolean> {
    return this.http.patch<boolean>(`${this.apiUrl}/backoffice/tenants/${id}/toggle-active`, {});
  }
}

