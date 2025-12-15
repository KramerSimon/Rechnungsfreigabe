import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CostCenter, Project } from '../models';

// Re-export for backward compatibility
export type { CostCenter, Project };

@Injectable({
  providedIn: 'root'
})
export class CostCenterService {
  private readonly apiUrl = `${environment.apiUrl}/costcenters`;

  constructor(private http: HttpClient) {}

  getAllCostCenters(): Observable<CostCenter[]> {
    return this.http.get<CostCenter[]>(this.apiUrl);
  }

  getCostCenterById(id: string): Observable<CostCenter> {
    return this.http.get<CostCenter>(`${this.apiUrl}/${id}`);
  }

  getProjectsForCostCenter(costCenterId: string): Observable<Project[]> {
    return this.http.get<Project[]>(`${this.apiUrl}/${costCenterId}/projects`);
  }
}
