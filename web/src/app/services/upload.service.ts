import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class UploadService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/uploads/avatar`;

  uploadAvatar(file: File): Observable<{ url: string }> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<{ url: string }>(this.base, form);
  }

  deleteAvatar(url: string): Observable<void> {
    const params: any = { url };
    return this.http.delete<void>(this.base, { params });
  }
}
