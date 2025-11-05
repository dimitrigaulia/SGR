import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { ShellComponent } from './shell/shell.component';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./components/login/login').then(m => m.Login)
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
      },
      {
        path: 'dashboard',
        loadComponent: () => import('./app').then(m => m.App)
      },
      {
        path: 'orcamentos',
        loadComponent: () => import('./app').then(m => m.App)
        // TODO: Substituir por componente real quando disponível
      },
      {
        path: 'propostas',
        loadComponent: () => import('./app').then(m => m.App)
        // TODO: Substituir por componente real quando disponível
      },
      {
        path: 'clientes',
        loadComponent: () => import('./app').then(m => m.App)
        // TODO: Substituir por componente real quando disponível
      },
      {
        path: 'config',
        loadComponent: () => import('./app').then(m => m.App)
        // TODO: Substituir por componente real quando disponível
      }
    ]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
