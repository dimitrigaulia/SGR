import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

/**
 * Service para exibiÃ§Ã£o de notificaÃ§Ãµes toast
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private snack = inject(MatSnackBar);

  /**
   * Exibe mensagem de sucesso
   */
  success(message: string, action = 'OK', duration = 3000) {
    this.snack.open(message, action, { duration, panelClass: ['snack-success'] });
  }

  /**
   * Exibe mensagem de erro
   */
  error(message: string, action = 'OK', duration = 4000) {
    this.snack.open(message, action, { duration, panelClass: ['snack-error'] });
  }

  /**
   * Exibe mensagem informativa
   */
  info(message: string, action = 'OK', duration = 3000) {
    this.snack.open(message, action, { duration, panelClass: ['snack-info'] });
  }
}

