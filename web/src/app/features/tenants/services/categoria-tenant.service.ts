import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { CategoriaTenantDto } from './tenant.service';

@Injectable({
  providedIn: 'root'
})
export class CategoriaTenantService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  /**
   * Busca todas as categorias ativas
   */
  getActive(): Observable<CategoriaTenantDto[]> {
    return this.http.get<CategoriaTenantDto[]>(`${this.apiUrl}/backoffice/categoriatenants/active`);
  }
}

