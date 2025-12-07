import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

/**
 * Interceptor para adicionar header X-Tenant-Subdomain nas requisiÃ§Ãµes do tenant
 */
export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  
  // Verificar se Ã© uma requisiÃ§Ã£o do tenant
  const context = authService.getContext();
  const tenantSubdomain = authService.getTenantSubdomain();
  
  // Adicionar header apenas se for contexto tenant e nÃ£o for rota do backoffice
  if (context === 'tenant' && tenantSubdomain && !req.url.includes('/api/backoffice/')) {
    req = req.clone({
      setHeaders: {
        'X-Tenant-Subdomain': tenantSubdomain
      }
    });
  }

  return next(req);
};

