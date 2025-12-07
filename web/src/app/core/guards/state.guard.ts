import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';

/**
 * Guard para validar state da navegaÃ§Ã£o (usado em formulÃ¡rios)
 */
export const stateGuard: CanActivateFn = (route) => {
  const router = inject(Router);
  const nav = router.getCurrentNavigation();
  const state: any = nav?.extras?.state ?? (typeof window !== 'undefined' ? (window as any).history?.state : undefined);
  
  // Se estiver tentando visualizar (view=true) mas sem id, bloquear
  if (state?.view && !state?.id) {
    const url = route.routeConfig?.path?.startsWith('perfis') ? '/perfis' : '/usuarios';
    return router.parseUrl(url);
  }
  
  return true;
};

