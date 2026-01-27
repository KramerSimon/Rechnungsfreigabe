import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CostCenter } from '../models/cost-center.model';
import { Project } from '../models/project.model';

@Injectable({
  providedIn: 'root'
})
export class CostCenterService {
  private readonly apiUrl = `${environment.apiUrl}/v1/cost-centers`;

  constructor(private http: HttpClient) {}

  getCostCenters(): Observable<CostCenter[]> {
    return this.http.get<CostCenter[]>(this.apiUrl);
  }

  getCostCenterById(id: string): Observable<CostCenter> {
    return this.http.get<CostCenter>(`${this.apiUrl}/${id}`);
  }

  getProjectsForCostCenter(costCenterId: string): Observable<Project[]> {
    return this.http.get<Project[]>(`${this.apiUrl}/${costCenterId}/projects`);
  }

  addCostCenter(costCenter: CostCenter): Observable<CostCenter> {
    return this.http.post<CostCenter>(this.apiUrl, costCenter);
  }

  deleteCostCenter(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  updateCostCenter(id: string, costCenter: CostCenter): Observable<CostCenter> {
    return this.http.put<CostCenter>(`${this.apiUrl}/${id}`, costCenter);
  }
}
