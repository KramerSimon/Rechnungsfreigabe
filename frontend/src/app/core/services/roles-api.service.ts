import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { RoleDto } from '../models/user.models';

export interface CreateRoleRequest {
  name: string;
  description?: string;
  permissions?: string[];
  color?: string;
}

export interface UpdateRoleRequest {
  name?: string;
  description?: string;
  permissions?: string[];
  color?: string;
}

@Injectable({ providedIn: 'root' })
export class RolesApiService {
  private readonly baseUrl = `${environment.apiUrl}/roles`;

  constructor(private http: HttpClient) {}

  getRoles(): Observable<RoleDto[]> {
    return this.http.get<RoleDto[]>(this.baseUrl);
  }

  createRole(payload: CreateRoleRequest): Observable<RoleDto> {
    return this.http.post<RoleDto>(this.baseUrl, payload);
  }

  updateRole(id: number, payload: UpdateRoleRequest): Observable<RoleDto> {
    return this.http.put<RoleDto>(`${this.baseUrl}/${id}`, payload);
  }

  deleteRole(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
