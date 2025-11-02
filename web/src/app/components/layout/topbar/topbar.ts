import { Component, inject } from '@angular/core';
import { CommonModule, AsyncPipe } from '@angular/common';
import { Router } from '@angular/router';
import { take } from 'rxjs/operators';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { LayoutService } from '../../../services/layout.service';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-topbar',
  imports: [
    CommonModule,
    AsyncPipe,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatDividerModule
  ],
  templateUrl: './topbar.html',
  styleUrl: './topbar.scss'
})
export class TopbarComponent {
  layoutService = inject(LayoutService);
  authService = inject(AuthService);
  router = inject(Router);

  get usuario() {
    return this.authService.getUsuario();
  }

  toggleSidebar(): void {
    // No mobile, usa o sidenav; no desktop, usa o toggle de collapse
    this.layoutService.isHandset$.pipe(take(1)).subscribe(isHandset => {
      if (isHandset) {
        // Mobile: toggle do sidenav (overlay)
        this.layoutService.toggleSidenav();
      } else {
        // Desktop: toggle do collapse (visual)
        this.layoutService.toggleSidebar();
      }
    });
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/']);
  }
}
