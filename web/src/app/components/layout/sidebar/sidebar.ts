import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { LayoutService } from '../../../services/layout.service';
import { MenuItem } from '../../../models/menu-item.model';

@Component({
  selector: 'app-sidebar',
  imports: [
    CommonModule,
    RouterModule,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule
  ],
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.scss'
})
export class SidebarComponent {
  layoutService = inject(LayoutService);
  
  menuItems: MenuItem[] = [
    {
      label: 'Dashboard',
      icon: 'dashboard',
      route: '/home'
    },
    {
      label: 'Usuários',
      icon: 'people',
      route: '/home/usuarios'
    },
    {
      label: 'Perfis',
      icon: 'admin_panel_settings',
      route: '/home/perfis'
    },
    {
      label: 'Restaurantes',
      icon: 'restaurant',
      route: '/home/restaurantes'
    }
  ];

  toggleSidebar(): void {
    this.layoutService.toggleSidebar();
  }
}
