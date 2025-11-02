import { Component, inject, OnInit, AfterViewInit, ViewChild, effect } from '@angular/core';
import { CommonModule, AsyncPipe } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { MatSidenavModule, MatSidenav } from '@angular/material/sidenav';
import { LayoutService } from '../../services/layout.service';
import { SidebarComponent } from './sidebar/sidebar';
import { TopbarComponent } from './topbar/topbar';

@Component({
  selector: 'app-layout',
  imports: [
    CommonModule,
    AsyncPipe,
    RouterOutlet,
    MatSidenavModule,
    SidebarComponent,
    TopbarComponent
  ],
  templateUrl: './layout.html',
  styleUrl: './layout.scss'
})
export class LayoutComponent implements OnInit, AfterViewInit {
  @ViewChild('sidenav') sidenav!: MatSidenav;
  
  layoutService = inject(LayoutService);
  breakpointObserver = inject(BreakpointObserver);

  ngOnInit(): void {
    // Ajustar sidebar baseado no tamanho da tela
    this.breakpointObserver
      .observe([Breakpoints.Handset, Breakpoints.TabletPortrait])
      .subscribe(result => {
        if (result.matches) {
          // Mobile/Tablet: sidebar como drawer (overlay)
          if (this.sidenav) {
            this.sidenav.mode = 'over';
            this.sidenav.close();
          }
        } else {
          // Desktop: sidebar como side (push)
          if (this.sidenav) {
            this.sidenav.mode = 'side';
            // Sempre manter aberto no desktop (o colapso é apenas visual)
            this.sidenav.open();
          }
        }
      });
  }

  ngAfterViewInit(): void {
    // Expor método toggle no service para uso no topbar (deve ser feito primeiro)
    if (this.sidenav) {
      this.layoutService.setSidenavReference(this.sidenav);
    }
    
    // Aguardar um tick para garantir que o sidenav esteja totalmente inicializado
    setTimeout(() => {
      if (this.sidenav) {
        this.layoutService.setSidenavReference(this.sidenav);
      }
    }, 0);
    
    // Sincronizar estado colapsado usando effect (após sidenav estar disponível)
    effect(() => {
      const collapsed = this.layoutService.sidebarCollapsed();
      if (this.sidenav && this.sidenav.mode === 'side') {
        // No modo 'side', sempre manter aberto
        if (!this.sidenav.opened) {
          this.sidenav.open();
        }
      }
    });
  }

  toggleSidebar(): void {
    if (this.sidenav) {
      this.sidenav.toggle();
    }
  }
}
