import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface InputDialogData {
  title?: string;
  message: string;
  placeholder?: string;
  initialValue?: string;
  confirmText?: string;
  cancelText?: string;
  required?: boolean;
}

@Component({
  selector: 'app-input-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  template: `
    <h2 mat-dialog-title>{{ data.title || 'Entrada de dados' }}</h2>
    <mat-dialog-content>
      <p style="margin-bottom: 16px;">{{ data.message }}</p>
      <mat-form-field appearance="outline" style="width: 100%;">
        <mat-label>{{ data.placeholder || 'Digite o valor' }}</mat-label>
        <input matInput [(ngModel)]="inputValue" [required]="data.required || false" />
      </mat-form-field>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button [mat-dialog-close]="null">
        {{ data.cancelText || 'Cancelar' }}
      </button>
      <button 
        mat-raised-button 
        color="primary" 
        [mat-dialog-close]="inputValue"
        [disabled]="(data.required || false) && !inputValue.trim()">
        {{ data.confirmText || 'Confirmar' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    mat-dialog-content {
      padding: 20px 24px;
      min-width: 300px;
    }
    mat-dialog-content p {
      margin: 0 0 16px 0;
      line-height: 1.5;
    }
    mat-dialog-actions {
      padding: 8px 24px 16px;
    }
  `]
})
export class InputDialogComponent {
  inputValue: string;

  constructor(
    public dialogRef: MatDialogRef<InputDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: InputDialogData
  ) {
    this.inputValue = data.initialValue || '';
  }
}

