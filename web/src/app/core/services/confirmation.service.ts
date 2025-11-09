import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Observable } from 'rxjs';
import { ConfirmationDialogComponent, ConfirmationDialogData } from '../../shared/components/confirmation-dialog/confirmation-dialog.component';

/**
 * Service para exibição de diálogos de confirmação
 * Segue o mesmo padrão do ToastService para consistência
 */
@Injectable({ providedIn: 'root' })
export class ConfirmationService {
  private dialog = inject(MatDialog);

  /**
   * Exibe um diálogo de confirmação customizado
   * @param data Dados do diálogo (mensagem, título, textos dos botões)
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
   * Método helper para confirmações simples
   * @param message Mensagem de confirmação
   * @param title Título do diálogo (opcional)
   * @returns Observable<boolean>
   */
  confirmSimple(message: string, title?: string): Observable<boolean> {
    return this.confirm({ message, title });
  }

  /**
   * Método helper para confirmações de exclusão
   * @param itemName Nome do item a ser excluído (opcional)
   * @returns Observable<boolean>
   */
  confirmDelete(itemName?: string): Observable<boolean> {
    const message = itemName 
      ? `Tem certeza que deseja excluir ${itemName}?`
      : 'Tem certeza que deseja excluir este item?';
    
    return this.confirm({
      message,
      title: 'Confirmar exclusão',
      confirmText: 'Excluir',
      cancelText: 'Cancelar',
      confirmColor: 'warn'
    });
  }

  /**
   * Método helper para confirmações de ativação/inativação
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

