import { Component, computed, inject, signal, ViewChild, AfterViewInit } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { NgFor, NgIf } from '@angular/common';
import { MatSidenav, MatSidenavModule } from '@angular/material/sidenav';

// Angular Material
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';

interface NavItem {
  icon: string;
  label: string;
  route: string;
}

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    NgFor,
    NgIf,
    MatSidenavModule,
    MatToolbarModule,
    MatIconModule,
    MatListModule,
    MatButtonModule,
    MatMenuModule,
    MatTooltipModule,
    MatDividerModule,
  ],
  templateUrl: './shell.component.html',
  styleUrls: ['./shell.component.scss']
})
export class ShellComponent implements AfterViewInit {
  @ViewChild('sidenav') sidenav!: MatSidenav;
  
  private bp = inject(BreakpointObserver);

  // Itens de navegação (ajuste conforme as rotas do SGR)
  readonly navItems: NavItem[] = [
    { icon: 'dashboard', label: 'Dashboard', route: '/dashboard' },
    { icon: 'description', label: 'Orçamentos', route: '/orcamentos' },
    { icon: 'assignment', label: 'Propostas', route: '/propostas' },
    { icon: 'groups', label: 'Clientes', route: '/clientes' },
    { icon: 'settings', label: 'Configurações', route: '/config' },
  ];

  // Estado
  readonly isHandset = signal(false);
  readonly sideCollapsed = signal(false);

  // Nome do usuário/empresa (exemplo)
  readonly brand = 'SGR';
  readonly currentYear = new Date().getFullYear();

  constructor() {
    this.bp.observe([Breakpoints.Handset]).subscribe(res => {
      this.isHandset.set(res.matches);
      if (res.matches) {
        this.sideCollapsed.set(true); // compacta em telas pequenas
      }
      // Ajustar sidenav após view estar pronta
      setTimeout(() => {
        if (this.sidenav) {
          if (res.matches) {
            this.sidenav.close();
          } else {
            this.sidenav.open();
          }
        }
      }, 0);
    });
  }

  ngAfterViewInit() {
    // Garantir que sidenav está aberto no desktop após inicialização
    if (!this.isHandset() && this.sidenav) {
      setTimeout(() => {
        if (this.sidenav && !this.isHandset()) {
          this.sidenav.open();
        }
      }, 0);
    }
  }

  toggleCollapse() {
    if (this.isHandset()) {
      // Mobile: toggle do drawer
      if (this.sidenav) {
        this.sidenav.toggle();
      }
    } else {
      // Desktop: apenas colapsar/expandir
      this.sideCollapsed.update(v => !v);
    }
  }

  onNavItemClick() {
    // Fechar sidenav no mobile após clicar em um item
    if (this.isHandset() && this.sidenav) {
      this.sidenav.close();
    }
  }

  // Modo layout pro MatSidenav
  readonly sidenavMode = computed(() => (this.isHandset() ? 'over' : 'side'));
  readonly sidenavOpened = computed(() => {
    if (this.isHandset()) {
      return false; // Mobile controla via toggle
    }
    return !this.sideCollapsed(); // Desktop baseado no estado collapsed
  });
}

