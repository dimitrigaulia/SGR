import { Component, computed, inject, signal, ViewChild, AfterViewInit, ChangeDetectionStrategy, DestroyRef, ChangeDetectorRef } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatSidenav, MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { MatExpansionModule } from '@angular/material/expansion';
import { CommonModule } from '@angular/common';
import { Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs';
import { AuthService } from '../core/services/auth.service';
import { LayoutService } from '../core/services/layout.service';

interface NavItem {
  icon: string;
  label: string;
  route?: string; // Opcional se tiver children
  children?: NavItem[]; // Subitens
}

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatSidenavModule,
    MatToolbarModule,
    MatIconModule,
    MatListModule,
    MatButtonModule,
    MatMenuModule,
    MatTooltipModule,
    MatDividerModule,
    MatExpansionModule,
  ],
  templateUrl: './shell.component.html',
  styleUrls: ['./shell.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ShellComponent implements AfterViewInit {
  @ViewChild('sidenav') sidenav!: MatSidenav;
  
  private bp = inject(BreakpointObserver);
  private router = inject(Router);
  private auth = inject(AuthService);
  private layout = inject(LayoutService);
  private destroyRef = inject(DestroyRef);
  private cdr = inject(ChangeDetectorRef);

  // Estado de expansão dos submenus (apenas para controle inicial)
  private _expandedMenus = new Set<string>();

  // Itens de navegação (será calculado baseado no contexto)
  readonly navItems = computed<NavItem[]>(() => {
    const context = this.auth.getContext();
    const baseUrl = context === 'backoffice' ? '/backoffice' : '/tenant';
    
    const items: NavItem[] = [
      { icon: 'dashboard', label: 'Dashboard', route: `${baseUrl}/dashboard` },
    ];

    // Itens específicos do backoffice
    if (context === 'backoffice') {
      items.push({
        icon: 'business',
        label: 'Gestão',
        children: [
          { icon: 'business', label: 'Tenants', route: `${baseUrl}/tenants` },
          { icon: 'people', label: 'Usuários', route: `${baseUrl}/usuarios` },
          { icon: 'badge', label: 'Perfis', route: `${baseUrl}/perfis` }
        ]
      });
    }

    // Itens específicos do tenant
        if (context === 'tenant') {
      items.push(
        {
          icon: 'inventory_2',
          label: 'Cadastros',
          children: [
            { icon: 'inventory_2', label: 'Insumos', route: `${baseUrl}/insumos` },
            { icon: 'restaurant', label: 'Receitas', route: `${baseUrl}/receitas` },
            { icon: 'description', label: 'Fichas Técnicas', route: `${baseUrl}/fichas-tecnicas` },
            { icon: 'category', label: 'Categorias de Insumo', route: `${baseUrl}/categorias-insumo` },
            { icon: 'category', label: 'Categorias de Receita', route: `${baseUrl}/categorias-receita` },
            { icon: 'straighten', label: 'Unidades de Medida', route: `${baseUrl}/unidades-medida` }
          ]
        },
        {
          icon: 'settings',
          label: 'Configurações',
          children: [
            { icon: 'people', label: 'Usuários', route: `${baseUrl}/usuarios` },
            { icon: 'badge', label: 'Perfis', route: `${baseUrl}/perfis` }
          ]
        }
      );
    }

    return items;
  });

  // Verifica se um item tem rota ativa
  isRouteActive(route: string): boolean {
    return this.router.url.startsWith(route);
  }

  // Verifica se algum child está ativo
  hasActiveChild(children?: NavItem[]): boolean {
    if (!children) return false;
    return children.some(child => child.route && this.isRouteActive(child.route));
  }

  // Verifica se menu deve estar expandido (apenas para inicialização)
  isMenuExpanded(menuLabel: string): boolean {
    return this._expandedMenus.has(menuLabel);
  }

  // Atualiza menus expandidos baseado nas rotas ativas
  private updateExpandedMenus() {
    const items = this.navItems();
    
    items.forEach(item => {
      if (item.children) {
        const shouldBeExpanded = this.hasActiveChild(item.children);
        const isExpanded = this._expandedMenus.has(item.label);
        
        if (shouldBeExpanded && !isExpanded) {
          this._expandedMenus.add(item.label);
        }
      }
    });
    
    this.cdr.markForCheck();
  }

  // Estado
  readonly isHandset = signal(false);
  readonly isCollapsed = signal(false);

  // Dados
  readonly brand = 'Cozintel';
  readonly currentYear = new Date().getFullYear();
  readonly themeIcon = computed(() => (this.layout.isDarkTheme() ? 'light_mode' : 'dark_mode'));
  readonly usuario = computed(() => this.auth.getUsuario());

  constructor() {
    // Observar breakpoints
    this.bp.observe([Breakpoints.Handset, Breakpoints.TabletPortrait])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(result => {
        const isMobile = result.matches;
        this.isHandset.set(isMobile);
        
        // Em mobile, sempre fechar sidenav
        if (isMobile && this.sidenav) {
          this.sidenav.close();
        } else if (!isMobile && this.sidenav) {
          // Em desktop, abrir se não estiver colapsado
          if (!this.isCollapsed()) {
            this.sidenav.open();
          }
        }
        this.cdr.markForCheck();
      });

    // Carregar estado do collapse do localStorage
    const savedCollapsed = localStorage.getItem('sidebarCollapsed');
    if (savedCollapsed !== null) {
      this.isCollapsed.set(JSON.parse(savedCollapsed));
    }

    // Expandir menus que têm rotas ativas (apenas quando necessário)
    this.router.events
      .pipe(
        filter(event => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => {
        // Aguardar próximo ciclo para garantir que os panels estejam disponíveis
        setTimeout(() => {
          this.updateExpandedMenus();
        }, 0);
      });
  }

  ngAfterViewInit() {
    // Garantir estado inicial correto
    if (!this.isHandset() && this.sidenav) {
      if (this.isCollapsed()) {
        this.sidenav.close();
      } else {
        this.sidenav.open();
      }
    }

    // Expandir menus que têm rotas ativas na inicialização
    setTimeout(() => {
      this.updateExpandedMenus();
    }, 0);
  }

  toggleSidenav() {
    if (this.isHandset()) {
      // Mobile: toggle simples
      this.sidenav?.toggle();
    } else {
      // Desktop: toggle collapse
      const newState = !this.isCollapsed();
      this.isCollapsed.set(newState);
      localStorage.setItem('sidebarCollapsed', JSON.stringify(newState));
      
      if (newState) {
        this.sidenav?.close();
      } else {
        this.sidenav?.open();
      }
    }
    this.cdr.markForCheck();
  }

  onNavItemClick() {
    // Fechar sidenav no mobile após clicar
    if (this.isHandset() && this.sidenav) {
      this.sidenav.close();
    }
  }

  // Computed para modo da sidenav
  readonly sidenavMode = computed(() => this.isHandset() ? 'over' : 'side');
  
  // Computed para se está aberta
  readonly sidenavOpened = computed(() => {
    if (this.isHandset()) {
      return false; // Mobile controla via toggle
    }
    return !this.isCollapsed(); // Desktop baseado no estado
  });

  // Ações do topo
  toggleTheme() {
    this.layout.toggleTheme();
  }

  onLogout() {
    this.auth.logout();
    this.router.navigate(['/']);
  }
}






