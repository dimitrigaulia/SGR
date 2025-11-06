import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { ToastService } from '../services/toast.service';
import { AuthService } from '../services/auth.service';

/**
 * Interceptor global para tratamento de erros HTTP
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const toast = inject(ToastService);
  const auth = inject(AuthService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let errorMessage = 'Ocorreu um erro inesperado';

      if (error.error instanceof ErrorEvent) {
        // Erro do lado do cliente
        errorMessage = `Erro: ${error.error.message}`;
      } else {
        // Erro do lado do servidor
        switch (error.status) {
          case 400:
            errorMessage = error.error?.message || 'Requisição inválida';
            break;
          case 401:
            errorMessage = 'Não autorizado. Faça login novamente.';
            auth.logout();
            router.navigate(['/']);
            break;
          case 403:
            errorMessage = 'Acesso negado';
            break;
          case 404:
            errorMessage = error.error?.message || 'Recurso não encontrado';
            break;
          case 409:
            errorMessage = error.error?.message || 'Conflito na operação';
            break;
          case 500:
            errorMessage = 'Erro interno do servidor. Tente novamente mais tarde.';
            break;
          default:
            errorMessage = error.error?.message || `Erro ${error.status}: ${error.statusText}`;
        }
      }

      // Exibir mensagem de erro apenas se não for 401 (já redireciona)
      if (error.status !== 401) {
        toast.error(errorMessage);
      }

      return throwError(() => error);
    })
  );
};

