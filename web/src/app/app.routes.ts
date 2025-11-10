import { Routes } from "@angular/router";
import { authGuard } from "./core/guards/auth.guard";
import { stateGuard } from "./core/guards/state.guard";
import { ShellComponent } from "./shell/shell.component";

export const routes: Routes = [
  // Rotas públicas de login
  { 
    path: "backoffice/login", 
    loadComponent: () => import("./backoffice/login/backoffice-login.component").then(m => m.BackofficeLoginComponent) 
  },
  { 
    path: "tenant/login", 
    loadComponent: () => import("./tenant/login/tenant-login.component").then(m => m.TenantLoginComponent) 
  },
  
  // Rotas do backoffice
  {
    path: "backoffice",
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: "", pathMatch: "full", redirectTo: "dashboard" },
      { path: "dashboard", loadComponent: () => import("./app").then(m => m.App) },
      { path: "usuarios", loadComponent: () => import("./backoffice/components/listagens/usuario/users-list.component").then(m => m.UsersListComponent) },
      { path: "usuarios/cadastro", canActivate: [stateGuard], loadComponent: () => import("./backoffice/components/cadastros/usuario/user-form.component").then(m => m.UserFormComponent) },
      { path: "perfis", loadComponent: () => import("./backoffice/components/listagens/perfil/perfis-list.component").then(m => m.PerfisListComponent) },
      { path: "perfis/cadastro", canActivate: [stateGuard], loadComponent: () => import("./backoffice/components/cadastros/perfil/perfil-form.component").then(m => m.PerfilFormComponent) },
      { path: "tenants", loadComponent: () => import("./backoffice/components/listagens/tenants/tenants-list.component").then(m => m.TenantsListComponent) },
      { path: "tenants/cadastro", canActivate: [stateGuard], loadComponent: () => import("./backoffice/components/cadastros/tenants/tenant-form.component").then(m => m.TenantFormComponent) },
    ]
  },
  
  // Rotas do tenant
  {
    path: "tenant",
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: "", pathMatch: "full", redirectTo: "dashboard" },
      { path: "dashboard", loadComponent: () => import("./app").then(m => m.App) },
      { path: "usuarios", loadComponent: () => import("./tenant/components/listagens/usuario/users-list.component").then(m => m.TenantUsersListComponent) },
      { path: "usuarios/cadastro", canActivate: [stateGuard], loadComponent: () => import("./tenant/components/cadastros/usuario/user-form.component").then(m => m.TenantUserFormComponent) },
      { path: "perfis", loadComponent: () => import("./tenant/components/listagens/perfil/perfis-list.component").then(m => m.TenantPerfisListComponent) },
      { path: "perfis/cadastro", canActivate: [stateGuard], loadComponent: () => import("./tenant/components/cadastros/perfil/perfil-form.component").then(m => m.TenantPerfilFormComponent) },
    ]
  },
  
  // Redirecionamento padrão
  { path: "", redirectTo: "backoffice/login", pathMatch: "full" },
  { path: "**", redirectTo: "backoffice/login" }
];
