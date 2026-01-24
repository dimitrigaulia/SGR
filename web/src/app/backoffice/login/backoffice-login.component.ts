import { Component, inject, DestroyRef, OnInit, effect } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';
import { LayoutService } from '../../core/services/layout.service';

/**
 * Componente de login do backoffice
 */
@Component({
  selector: 'app-backoffice-login',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './backoffice-login.component.html',
  styleUrl: './backoffice-login.component.scss',
  host: {
    '[class.dark-theme]': 'isDarkTheme()',
    '[class.light-theme]': '!isDarkTheme()'
  }
})
export class BackofficeLoginComponent implements OnInit {
  private authService = inject(AuthService);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  private layoutService = inject(LayoutService);

  model = {
    email: '',
    senha: ''
  };
  
  isLoading = false;
  errorMessage = '';
  isDarkTheme = this.layoutService.isDarkTheme;

  constructor() {
    // Atualizar classe do body quando o tema mudar
    effect(() => {
      const isDark = this.layoutService.isDarkTheme();
      document.documentElement.classList.toggle('dark-theme', isDark);
      document.documentElement.classList.toggle('light-theme', !isDark);
    });
  }

  ngOnInit(): void {
    // Inicializar tema baseado no sistema ou preferência salva
    const savedTheme = localStorage.getItem('themeMode') as 'dark' | 'light' | null;
    if (savedTheme) {
      this.layoutService.setTheme(savedTheme);
    } else {
      // Detectar tema do sistema
      const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
      this.layoutService.setTheme(prefersDark ? 'dark' : 'light');
    }
  }

  toggleTheme(): void {
    this.layoutService.toggleTheme();
  }

  onSubmit(form: any): void {
    if (form.invalid) {
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    // Login do backoffice usa endpoint especÃ­fico
    this.authService.loginBackoffice(this.model)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isLoading = false;
          this.router.navigate(['/backoffice/dashboard']);
        },
        error: (error) => {
          this.isLoading = false;
          this.errorMessage = error.error?.message || 'Email ou senha inválidos.';
        }
      });
  }
}

