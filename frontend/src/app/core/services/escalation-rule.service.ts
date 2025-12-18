import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateEscalationRuleDto, EscalationRule } from '../models/escalation-rule.model';

@Injectable({ providedIn: 'root' })
export class EscalationRuleService {
  private readonly apiUrl = `${environment.apiUrl}/escalation-rules`;

  constructor(private http: HttpClient) {}

  getRules(): Observable<EscalationRule[]> {
    return this.http.get<EscalationRule[]>(this.apiUrl);
  }

  getRule(id: number): Observable<EscalationRule> {
    return this.http.get<EscalationRule>(`${this.apiUrl}/${id}`);
  }

  createRule(payload: CreateEscalationRuleDto): Observable<EscalationRule> {
    return this.http.post<EscalationRule>(this.apiUrl, payload);
  }

  updateRule(id: number, payload: CreateEscalationRuleDto): Observable<EscalationRule> {
    return this.http.put<EscalationRule>(`${this.apiUrl}/${id}`, payload);
  }

  deleteRule(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
