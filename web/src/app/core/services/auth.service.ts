import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginRequest, LoginResponse } from '../models/auth.model';

/**
 * Service para gerenciamento de autenticação
 */
@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  /**
   * Realiza login do backoffice
   */
  loginBackoffice(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/backoffice/auth/login`, request).pipe(
      tap(response => {
        // Salvar token no localStorage
        if (response.token) {
          localStorage.setItem('token', response.token);
          localStorage.setItem('usuario', JSON.stringify(response.usuario));
          localStorage.setItem('perfil', JSON.stringify(response.perfil));
          localStorage.setItem('context', 'backoffice');
          localStorage.removeItem('tenantSubdomain');
        }
      })
    );
  }

  /**
   * Realiza login do tenant
   */
  loginTenant(request: LoginRequest, tenantSubdomain: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/tenant/auth/login`, request, {
      headers: {
        'X-Tenant-Subdomain': tenantSubdomain
      }
    }).pipe(
      tap(response => {
        // Salvar token no localStorage
        if (response.token) {
          localStorage.setItem('token', response.token);
          localStorage.setItem('usuario', JSON.stringify(response.usuario));
          localStorage.setItem('perfil', JSON.stringify(response.perfil));
          localStorage.setItem('context', 'tenant');
          localStorage.setItem('tenantSubdomain', tenantSubdomain);
        }
      })
    );
  }

  /**
   * Realiza login do usuário (método genérico - mantido para compatibilidade)
   */
  login(request: LoginRequest): Observable<LoginResponse> {
    return this.loginBackoffice(request);
  }

  /**
   * Realiza logout do usuário
   */
  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('usuario');
    localStorage.removeItem('perfil');
    localStorage.removeItem('context');
    localStorage.removeItem('tenantSubdomain');
  }

  /**
   * Obtém o contexto atual (backoffice ou tenant)
   */
  getContext(): 'backoffice' | 'tenant' | null {
    return localStorage.getItem('context') as 'backoffice' | 'tenant' | null;
  }

  /**
   * Obtém o subdomínio do tenant atual
   */
  getTenantSubdomain(): string | null {
    return localStorage.getItem('tenantSubdomain');
  }

  /**
   * Obtém o token JWT armazenado
   */
  getToken(): string | null {
    return localStorage.getItem('token');
  }

  /**
   * Verifica se o usuário está autenticado
   */
  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  /**
   * Obtém os dados do usuário logado
   */
  getUsuario(): any {
    const usuarioStr = localStorage.getItem('usuario');
    return usuarioStr ? JSON.parse(usuarioStr) : null;
  }
}

