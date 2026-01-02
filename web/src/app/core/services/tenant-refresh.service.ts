import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

/**
 * Serviço para notificar atualizações na lista de tenants
 * Permite que componentes CRUD notifiquem o TenantSelector para recarregar a lista
 */
@Injectable({
  providedIn: 'root'
})
export class TenantRefreshService {
  private refreshSubject = new Subject<void>();

  /**
   * Observable para se inscrever em eventos de refresh
   */
  get refresh$() {
    return this.refreshSubject.asObservable();
  }

  /**
   * Notifica que a lista de tenants deve ser atualizada
   */
  notifyRefresh(): void {
    this.refreshSubject.next();
  }
}
