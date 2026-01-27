import { Component, Input, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ApprovalService } from '../../core/services/approval.service';
import { ApprovalWorkflow } from '../../core/models/approval.model';

@Component({
  selector: 'app-approval-timeline',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule],
  template: `
    <mat-card class="timeline-card">
      <mat-card-header>
        <mat-card-title>Genehmigungsablauf</mat-card-title>
        <mat-card-subtitle>Stufen und Status je Rechnung</mat-card-subtitle>
      </mat-card-header>
      <mat-card-content>
        <div *ngIf="loading" class="timeline-loading">
          <mat-icon>hourglass_empty</mat-icon>
          <span>Lade Genehmigungsschritte...</span>
        </div>

        <div *ngIf="error" class="timeline-error">
          <mat-icon color="warn">error</mat-icon>
          <span>{{ error }}</span>
        </div>

        <div *ngIf="!loading && !error && steps.length === 0" class="timeline-empty">
          <mat-icon>timeline</mat-icon>
          <span>Keine Genehmigungsschritte vorhanden</span>
        </div>

        <div *ngIf="!loading && !error && steps.length > 0" class="timeline">
          <div *ngFor="let s of steps; let i = index" class="timeline-step" [ngClass]="statusClass(s.status)">
            <div class="step-number">{{ s.stepNumber }}</div>
            <div class="step-content">
              <div class="step-header">
                <span class="status-badge" [ngClass]="'status-' + (s.status || '').toLowerCase()">{{ s.status }}</span>
                <span class="approver">{{ s.approverName || s.approverId }}</span>
              </div>
              <div class="step-meta">
                <span>Stufe: {{ s.approvalLevel }}</span>
                <span *ngIf="s.approvedAt">• {{ s.approvedAt | date:'dd.MM.yyyy HH:mm' }}</span>
              </div>
            </div>
            <div class="connector" *ngIf="i < steps.length - 1">
              <span class="line"></span>
            </div>
          </div>
        </div>
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    .timeline-card { margin-top: 16px; }
    .timeline-loading, .timeline-empty {
      display: flex; align-items: center; gap: 8px; color: #666; padding: 8px 0;
    }
    .timeline { display: flex; align-items: stretch; gap: 16px; overflow-x: auto; padding: 8px 0; }
    .timeline-step {
      display: flex; align-items: center; position: relative; background: #fafafa; border: 1px solid #eee; border-radius: 8px; padding: 8px 12px;
    }
    .step-number {
      width: 28px; height: 28px; border-radius: 50%; background: #1976d2; color: #fff; display: flex; align-items: center; justify-content: center; font-weight: 600; margin-right: 10px;
    }
    .step-content { min-width: 220px; }
    .step-header { display: flex; align-items: center; gap: 8px; margin-bottom: 4px; }
    .approver { color: #333; font-weight: 500; }
    .step-meta { font-size: 12px; color: #777; }
    .connector { display: flex; align-items: center; }
    .connector .line { width: 24px; height: 2px; background: #ddd; margin-left: 8px; margin-right: 8px; }

    .status-badge { display: inline-block; padding: 2px 8px; border-radius: 12px; font-size: 12px; font-weight: 600; color: #fff; }
    .status-approved { background-color: #4caf50; }
    .status-rejected { background-color: #f44336; }
    .status-pending  { background-color: #ff9800; }
    .status-skipped  { background-color: #9e9e9e; }
    .status-waiting  { background-color: #607d8b; }

    .timeline-step.status-approved { border-color: #c8e6c9; }
    .timeline-step.status-rejected { border-color: #ffcdd2; }
    .timeline-step.status-pending  { border-color: #ffe0b2; }
    .timeline-step.status-waiting  { border-color: #cfd8dc; }
    .timeline-step.status-skipped  { border-color: #e0e0e0; }

    .timeline-error { display: flex; align-items: center; gap: 8px; color: #d32f2f; padding: 8px 0; }
  `]
})
export class ApprovalTimelineComponent implements OnInit, OnChanges {
  @Input() invoiceId!: number;
  @Input() workflows: ApprovalWorkflow[] | null = null;

  loading = false;
  steps: ApprovalWorkflow[] = [];
  error: string | null = null;

  constructor(private approvalService: ApprovalService) {}

  ngOnInit(): void {
    this.refresh();
  }

  ngOnChanges(changes: SimpleChanges): void {
    // Refresh whenever either the invoiceId or provided workflows change
    if (changes['invoiceId'] || changes['workflows']) {
      this.refresh();
    }
  }

  private refresh(): void {
    // Prefer data passed in with the invoice to avoid extra calls
    if (this.workflows && this.workflows.length) {
      this.error = null;
      this.steps = [...this.workflows].sort((a, b) => a.stepNumber - b.stepNumber);
      return;
    }

    // If nothing to load or no invoice id, show empty state; backend already embeds pendingApprovals in invoice DTO
    this.steps = [];
  }

  statusClass(status?: string): string {
    const s = (status || '').toLowerCase();
    return `status-${s}`;
  }
}
