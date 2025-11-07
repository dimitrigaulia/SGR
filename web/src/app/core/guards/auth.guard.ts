import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Guard para proteger rotas que requerem autenticação
 */
export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    // Verificar se o contexto da rota corresponde ao contexto do usuário
    const url = state.url;
    const context = authService.getContext();
    
    if (url.startsWith('/backoffice') && context !== 'backoffice') {
      router.navigate(['/backoffice/login']);
      return false;
    }
    
    if (url.startsWith('/tenant') && context !== 'tenant') {
      router.navigate(['/tenant/login']);
      return false;
    }
    
    return true;
  }

  // Redirecionar para login baseado na rota
  if (state.url.startsWith('/backoffice')) {
    router.navigate(['/backoffice/login']);
  } else if (state.url.startsWith('/tenant')) {
    router.navigate(['/tenant/login']);
  } else {
    router.navigate(['/backoffice/login']);
  }
  
  return false;
};

