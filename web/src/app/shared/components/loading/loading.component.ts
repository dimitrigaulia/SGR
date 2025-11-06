import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

/**
 * Componente de loading compartilhado para uso em todo o sistema
 * Usa o estilo global .loading-container definido em styles.scss
 */
@Component({
  selector: 'app-loading',
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule],
  template: `
    <div class="loading-container">
      <mat-spinner [diameter]="diameter"></mat-spinner>
    </div>
  `
})
export class LoadingComponent {
  /**
   * Diâmetro do spinner (padrão: 50)
   */
  @Input() diameter: number = 50;
}

