import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginRequest, LoginResponse } from '../models/auth.model';

/**
 * Service para gerenciamento de autenticaÃ§Ã£o
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
          // Limpar estado de impersonação ao fazer login
          localStorage.removeItem('impersonatedTenant');
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
          // Limpar estado de impersonação ao fazer login
          localStorage.removeItem('impersonatedTenant');
        }
      })
    );
  }

  /**
   * Realiza login do usuÃ¡rio (mÃ©todo genÃ©rico - mantido para compatibilidade)
   */
  login(request: LoginRequest): Observable<LoginResponse> {
    return this.loginBackoffice(request);
  }

  /**
   * Realiza logout do usuÃ¡rio
   */
  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('usuario');
    localStorage.removeItem('perfil');
    localStorage.removeItem('context');
    localStorage.removeItem('tenantSubdomain');
    localStorage.removeItem('impersonatedTenant');
  }

  /**
   * Define o tenant sendo impersonado pelo backoffice
   */
  setImpersonatedTenant(tenantSubdomain: string): void {
    if (this.getContext() === 'backoffice') {
      localStorage.setItem('impersonatedTenant', tenantSubdomain);
    }
  }

  /**
   * Obtém o tenant sendo impersonado
   */
  getImpersonatedTenant(): string | null {
    return localStorage.getItem('impersonatedTenant');
  }

  /**
   * Limpa a impersonação
   */
  clearImpersonation(): void {
    localStorage.removeItem('impersonatedTenant');
  }

  /**
   * Verifica se está em modo de impersonação
   */
  isImpersonating(): boolean {
    return this.getContext() === 'backoffice' && !!this.getImpersonatedTenant();
  }

  /**
   * ObtÃ©m o contexto atual (backoffice ou tenant)
   */
  getContext(): 'backoffice' | 'tenant' | null {
    return localStorage.getItem('context') as 'backoffice' | 'tenant' | null;
  }

  /**
   * ObtÃ©m o subdomÃ­nio do tenant atual
   */
  getTenantSubdomain(): string | null {
    return localStorage.getItem('tenantSubdomain');
  }

  /**
   * ObtÃ©m o token JWT armazenado
   */
  getToken(): string | null {
    return localStorage.getItem('token');
  }

  /**
   * Verifica se o usuÃ¡rio estÃ¡ autenticado
   */
  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  /**
   * ObtÃ©m os dados do usuÃ¡rio logado
   */
  getUsuario(): any {
    const usuarioStr = localStorage.getItem('usuario');
    return usuarioStr ? JSON.parse(usuarioStr) : null;
  }
}

