import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Guard para proteger rotas que requerem autenticaÃ§Ã£o
 */
export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    // Verificar se o contexto da rota corresponde ao contexto do usuÃ¡rio
    const url = state.url;
    const context = authService.getContext();
    const isImpersonating = authService.isImpersonating();
    
    if (url.startsWith('/backoffice') && context !== 'backoffice') {
      router.navigate(['/backoffice/login']);
      return false;
    }
    
    // Permitir acesso às rotas do tenant se:
    // 1. O contexto for 'tenant' (login normal do tenant), OU
    // 2. O contexto for 'backoffice' e estiver impersonando um tenant
    if (url.startsWith('/tenant')) {
      if (context === 'tenant') {
        return true;
      } else if (context === 'backoffice' && isImpersonating) {
        return true;
      } else {
        router.navigate(['/tenant/login']);
        return false;
      }
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

