import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface UploadResponse {
  url: string;
}

/**
 * Service para upload de arquivos
 */
@Injectable({
  providedIn: 'root'
})
export class UploadService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  /**
   * Faz upload de um avatar
   */
  uploadAvatar(file: File): Observable<UploadResponse> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<UploadResponse>(`${this.apiUrl}/uploads/avatar`, formData);
  }

  /**
   * Remove um avatar
   */
  deleteAvatar(url: string): Observable<void> {
    const fileName = url.split('/').pop();
    return this.http.delete<void>(`${this.apiUrl}/uploads/avatar`, {
      params: { name: fileName || '' }
    });
  }
}

