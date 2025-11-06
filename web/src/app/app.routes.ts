import { Routes } from "@angular/router";
import { authGuard } from "./core/guards/auth.guard";
import { stateGuard } from "./core/guards/state.guard";
import { ShellComponent } from "./shell/shell.component";

export const routes: Routes = [
  { path: "", loadComponent: () => import("./components/login/login").then(m => m.Login) },
  {
    path: "",
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: "", pathMatch: "full", redirectTo: "dashboard" },
      { path: "dashboard", loadComponent: () => import("./app").then(m => m.App) },
      { path: "usuarios", loadComponent: () => import("./components/listagens/usuario/users-list.component").then(m => m.UsersListComponent) },
      { path: "usuarios/cadastro", canActivate: [stateGuard], loadComponent: () => import("./components/cadastros/usuario/user-form.component").then(m => m.UserFormComponent) },
      { path: "perfis", loadComponent: () => import("./components/listagens/perfil/perfis-list.component").then(m => m.PerfisListComponent) },
      { path: "perfis/cadastro", canActivate: [stateGuard], loadComponent: () => import("./components/cadastros/perfil/perfil-form.component").then(m => m.PerfilFormComponent) },
    ]
  },
  { path: "**", redirectTo: "" }
];
