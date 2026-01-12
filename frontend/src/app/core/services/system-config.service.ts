import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, map, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SystemConfig, UpsertSystemConfig } from '../models';

@Injectable({ providedIn: 'root' })
export class SystemConfigService {
  private readonly apiUrl = `${environment.apiUrl}/system-config`;

  constructor(private http: HttpClient) {}

  getAll() {
    return this.http.get<SystemConfig[]>(this.apiUrl).pipe(
      catchError((error) => {
        console.error('Error loading system configurations:', error);
        return of([] as SystemConfig[]);
      })
    );
  }

  getByKey(key: string) {
    return this.http.get<SystemConfig>(`${this.apiUrl}/${encodeURIComponent(key)}`).pipe(
      catchError((error) => {
        console.error('Error loading configuration:', error);
        return of(null);
      })
    );
  }

  upsert(key: string, payload: UpsertSystemConfig) {
    return this.http.put<SystemConfig>(`${this.apiUrl}/${encodeURIComponent(key)}`, payload).pipe(
      catchError((error) => {
        console.error('Error saving configuration:', error);
        throw error;
      })
    );
  }
}
