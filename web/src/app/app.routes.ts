import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./components/login/login').then(m => m.Login)
  },
  {
    path: 'home',
    canActivate: [authGuard],
    loadComponent: () => import('./app').then(m => m.App)
  },
  {
    path: '**',
    redirectTo: ''
  }
];
