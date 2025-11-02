import { Injectable, signal, inject } from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { MatSidenav } from '@angular/material/sidenav';
import { Observable, map, shareReplay } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class LayoutService {
  private _sidebarCollapsed = signal<boolean>(false);
  private sidenav?: MatSidenav;
  private breakpointObserver = inject(BreakpointObserver);

  sidebarCollapsed = this._sidebarCollapsed.asReadonly();

  // Observar breakpoints para responsividade
  isHandset$: Observable<boolean>;
  isTablet$: Observable<boolean>;
  isDesktop$: Observable<boolean>;

  constructor() {
    // Inicializar observables no construtor
    this.isHandset$ = this.breakpointObserver
      .observe([Breakpoints.Handset, Breakpoints.TabletPortrait])
      .pipe(
        map(result => result.matches),
        shareReplay()
      );

    this.isTablet$ = this.breakpointObserver
      .observe(Breakpoints.Tablet)
      .pipe(
        map(result => result.matches),
        shareReplay()
      );

    this.isDesktop$ = this.breakpointObserver
      .observe([Breakpoints.Web, Breakpoints.TabletLandscape])
      .pipe(
        map(result => result.matches),
        shareReplay()
      );

    // Carregar estado salvo do localStorage
    const savedState = localStorage.getItem('sidebarCollapsed');
    if (savedState !== null) {
      this._sidebarCollapsed.set(JSON.parse(savedState));
    }
  }

  toggleSidebar(): void {
    this._sidebarCollapsed.update(current => {
      const newValue = !current;
      localStorage.setItem('sidebarCollapsed', JSON.stringify(newValue));
      return newValue;
    });
  }

  setSidebarCollapsed(collapsed: boolean): void {
    this._sidebarCollapsed.set(collapsed);
    localStorage.setItem('sidebarCollapsed', JSON.stringify(collapsed));
  }

  setSidenavReference(sidenav: MatSidenav): void {
    this.sidenav = sidenav;
  }

  toggleSidenav(): void {
    if (this.sidenav) {
      this.sidenav.toggle();
    } else {
      console.warn('Sidenav não está disponível');
    }
  }
}

