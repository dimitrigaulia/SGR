import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({ providedIn: 'root' })
export class ToastService {
  private snack = inject(MatSnackBar);

  success(message: string, action = 'OK', duration = 3000) {
    this.snack.open(message, action, { duration, panelClass: ['snack-success'] });
  }

  error(message: string, action = 'OK', duration = 4000) {
    this.snack.open(message, action, { duration, panelClass: ['snack-error'] });
  }

  info(message: string, action = 'OK', duration = 3000) {
    this.snack.open(message, action, { duration, panelClass: ['snack-info'] });
  }
}

