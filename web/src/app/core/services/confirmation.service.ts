import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Observable } from 'rxjs';
import { ConfirmationDialogComponent, ConfirmationDialogData } from '../../shared/components/confirmation-dialog/confirmation-dialog.component';

/**
 * Service para exibiÃ§Ã£o de diÃ¡logos de confirmaÃ§Ã£o
 * Segue o mesmo padrÃ£o do ToastService para consistÃªncia
 */
@Injectable({ providedIn: 'root' })
export class ConfirmationService {
  private dialog = inject(MatDialog);

  /**
   * Exibe um diÃ¡logo de confirmaÃ§Ã£o customizado
   * @param data Dados do diÃ¡logo (mensagem, tÃ­tulo, textos dos botÃµes)
   * @returns Observable<boolean> - true se confirmado, false se cancelado
   */
  confirm(data: ConfirmationDialogData): Observable<boolean> {
    const dialogRef = this.dialog.open(ConfirmationDialogComponent, {
      width: '400px',
      data,
      disableClose: false,
      autoFocus: true
    });

    return dialogRef.afterClosed();
  }

  /**
   * MÃ©todo helper para confirmaÃ§Ãµes simples
   * @param message Mensagem de confirmaÃ§Ã£o
   * @param title TÃ­tulo do diÃ¡logo (opcional)
   * @returns Observable<boolean>
   */
  confirmSimple(message: string, title?: string): Observable<boolean> {
    return this.confirm({ message, title });
  }

  /**
   * MÃ©todo helper para confirmaÃ§Ãµes de exclusÃ£o
   * @param itemName Nome do item a ser excluÃ­do (opcional)
   * @returns Observable<boolean>
   */
  confirmDelete(itemName?: string): Observable<boolean> {
    const message = itemName 
      ? `Tem certeza que deseja excluir ${itemName}?`
      : 'Tem certeza que deseja excluir este item?';
    
    return this.confirm({
      message,
      title: 'Confirmar exclusÃ£o',
      confirmText: 'Excluir',
      cancelText: 'Cancelar',
      confirmColor: 'warn'
    });
  }

  /**
   * MÃ©todo helper para confirmaÃ§Ãµes de ativaÃ§Ã£o/inativaÃ§Ã£o
   * @param action 'ativar' ou 'inativar'
   * @param itemName Nome do item (opcional)
   * @param warningMessage Mensagem de aviso adicional (opcional)
   * @returns Observable<boolean>
   */
  confirmToggleActive(
    action: 'ativar' | 'inativar', 
    itemName?: string,
    warningMessage?: string
  ): Observable<boolean> {
    const baseMessage = itemName
      ? `Tem certeza que deseja ${action} ${itemName}?`
      : `Tem certeza que deseja ${action} este item?`;
    
    const message = warningMessage 
      ? `${baseMessage} ${warningMessage}`
      : baseMessage;

    return this.confirm({
      message,
      title: `Confirmar ${action}`,
      confirmText: action === 'inativar' ? 'Inativar' : 'Ativar',
      cancelText: 'Cancelar',
      confirmColor: action === 'inativar' ? 'warn' : 'primary'
    });
  }
}

