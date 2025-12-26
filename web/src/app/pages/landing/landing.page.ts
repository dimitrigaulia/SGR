import { Component, OnInit, inject, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { LayoutService } from '../../core/services/layout.service';

@Component({
  selector: 'app-landing-page',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './landing.page.html',
  styleUrl: './landing.page.scss'
})
export class LandingPageComponent implements OnInit {
  private layoutService = inject(LayoutService);
  isMenuOpen = false;
  currentYear = new Date().getFullYear();
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

  toggleMenu(): void {
    this.isMenuOpen = !this.isMenuOpen;
  }

  closeMenu(): void {
    this.isMenuOpen = false;
  }

  scrollToSection(sectionId: string): void {
    const element = document.getElementById(sectionId);
    if (element) {
      element.scrollIntoView({ behavior: 'smooth', block: 'start' });
      this.closeMenu();
    }
  }
}

