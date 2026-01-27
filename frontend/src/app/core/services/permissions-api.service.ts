import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface PermissionDto {
  id: number;
  name: string;
  description?: string;
  code: string;
  category?: string;
  isSystemPermission: boolean;
}

@Injectable({ providedIn: 'root' })
export class PermissionsApiService {
  private readonly baseUrl = `${environment.apiUrl}/v1/permissions`;

  constructor(private http: HttpClient) {}

  getPermissions(): Observable<PermissionDto[]> {
    return this.http.get<PermissionDto[]>(this.baseUrl);
  }

  getPermissionById(id: number): Observable<PermissionDto> {
    return this.http.get<PermissionDto>(`${this.baseUrl}/${id}`);
  }

  createPermission(payload: Partial<PermissionDto>): Observable<PermissionDto> {
    return this.http.post<PermissionDto>(this.baseUrl, payload);
  }

  updatePermission(id: number, payload: Partial<PermissionDto>): Observable<PermissionDto> {
    return this.http.put<PermissionDto>(`${this.baseUrl}/${id}`, payload);
  }

  deletePermission(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
