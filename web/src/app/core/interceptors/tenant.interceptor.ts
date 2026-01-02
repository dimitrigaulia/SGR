import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

/**
 * Interceptor para adicionar header X-Tenant-Subdomain nas requisiÃ§Ãµes do tenant
 * Também adiciona headers de impersonação quando o backoffice está impersonando um tenant
 */
export const tenantInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  
  // Verificar se Ã© uma requisiÃ§Ã£o do tenant
  const context = authService.getContext();
  const tenantSubdomain = authService.getTenantSubdomain();
  const impersonatedTenant = authService.getImpersonatedTenant();
  const isImpersonating = authService.isImpersonating();
  
  // Não adicionar headers para rotas do backoffice
  if (req.url.includes('/api/backoffice/')) {
    return next(req);
  }

  // Se for contexto tenant normal, adicionar header X-Tenant-Subdomain
  if (context === 'tenant' && tenantSubdomain) {
    req = req.clone({
      setHeaders: {
        'X-Tenant-Subdomain': tenantSubdomain
      }
    });
  }
  
  // Se for impersonação do backoffice, adicionar ambos os headers
  if (isImpersonating && impersonatedTenant && context === 'backoffice') {
    req = req.clone({
      setHeaders: {
        'X-Tenant-Subdomain': impersonatedTenant,
        'X-Backoffice-Impersonation': 'true'
      }
    });
  }

  return next(req);
};

