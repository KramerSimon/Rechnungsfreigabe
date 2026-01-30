import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApprovalRule, ApprovalWorkflow, CreateApprovalRuleDto, CreateApprovalWorkflowDto, UpdateApprovalRuleDto, UpdateApprovalWorkflowDto } from '../models/approval.model';

@Injectable({
  providedIn: 'root'
})
export class ApprovalService {
  private apiUrl = `${environment.apiUrl}/v1/approvals`;

  constructor(private http: HttpClient) {}

  // Rules
  getApprovalRules(): Observable<ApprovalRule[]> {
    return this.http.get<ApprovalRule[]>(`${this.apiUrl}/rules`);
  }

  createApprovalRule(rule: CreateApprovalRuleDto): Observable<ApprovalRule> {
    return this.http.post<ApprovalRule>(`${this.apiUrl}/rules`, rule);
  }

  updateApprovalRule(id: number, rule: UpdateApprovalRuleDto): Observable<ApprovalRule> {
    return this.http.put<ApprovalRule>(`${this.apiUrl}/rules/${id}`, rule);
  }

  deleteApprovalRule(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/rules/${id}`);
  }

  // Workflows
  getApprovalWorkflows(): Observable<ApprovalWorkflow[]> {
    return this.http.get<ApprovalWorkflow[]>(`${this.apiUrl}/workflows`);
  }

  getApprovalWorkflowsForInvoice(invoiceId: number): Observable<ApprovalWorkflow[]> {
    // Prefer server-side filter if supported; otherwise client can filter
    return this.http.get<ApprovalWorkflow[]>(`${this.apiUrl}/workflows`, {
      params: { invoiceId: String(invoiceId) }
    });
  }

  getApprovalWorkflowById(id: number): Observable<ApprovalWorkflow> {
    return this.http.get<ApprovalWorkflow>(`${this.apiUrl}/workflows/${id}`);
  }

  createApprovalWorkflow(payload: CreateApprovalWorkflowDto): Observable<ApprovalWorkflow> {
    return this.http.post<ApprovalWorkflow>(`${this.apiUrl}/workflows`, payload);
  }

  updateApprovalWorkflow(id: number, payload: UpdateApprovalWorkflowDto): Observable<ApprovalWorkflow> {
    return this.http.put<ApprovalWorkflow>(`${this.apiUrl}/workflows/${id}`, payload);
  }

  deleteApprovalWorkflow(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/workflows/${id}`);
  }

  getPendingApprovals(): Observable<ApprovalWorkflow[]> {
    return this.http.get<ApprovalWorkflow[]>(`${this.apiUrl}/pending`);
  }

  approveWorkflow(id: number, comments?: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/approve`, { comments });
  }

  rejectWorkflow(id: number, comments?: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/reject`, { comments });
  }
}
