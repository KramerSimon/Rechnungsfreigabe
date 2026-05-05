import { Component, Input, OnChanges, OnInit, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { ApprovalService } from '../../core/services/approval.service';
import { ApprovalWorkflow } from '../../core/models/approval.model';
import { StatusTranslatorService } from '../../core/services/status-translator.service';
import { LanguageService } from '../../core/services/language.service';

@Component({
  selector: 'app-approval-timeline',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule],
  template: `
    <mat-card class="timeline-card">
      <mat-card-header>
        <mat-card-title>{{ t('invoice.approval.title') }}</mat-card-title>
        <mat-card-subtitle>{{ t('invoice.approval.subtitle') }}</mat-card-subtitle>
      </mat-card-header>
      <mat-card-content>
        <div *ngIf="loading" class="timeline-loading">
          <mat-icon>hourglass_empty</mat-icon>
          <span>{{ t('invoice.approval.loading') }}</span>
        </div>

        <div *ngIf="error" class="timeline-error">
          <mat-icon color="warn">error</mat-icon>
          <span>{{ error }}</span>
        </div>

        <div *ngIf="!loading && !error && steps.length === 0" class="timeline-empty">
          <mat-icon>timeline</mat-icon>
          <span>{{ t('invoice.approval.empty') }}</span>
        </div>

        <div *ngIf="!loading && !error && steps.length > 0" class="timeline">
          <div *ngFor="let s of steps; let i = index"
               class="timeline-step"
               [ngStyle]="s.statusColor ? {
                 'background-color': s.statusColor,
                 'border-color': s.statusColor
               } : {}">
            <div class="step-number">{{ s.stepNumber }}</div>
            <div class="step-content">
              <div class="step-header">
                <span class="status-label" [ngStyle]="s.statusColor ? { 'color': getContrastColor(s.statusColor) } : {}">
                  {{ translateStatus(s.status) }}
                </span>
                <span class="approver" [ngStyle]="s.statusColor ? { 'color': getContrastColor(s.statusColor) } : {}">
                  {{ s.approverName || s.approverId }}
                </span>
              </div>
              <div class="step-meta" [ngStyle]="s.statusColor ? { 'color': getContrastColor(s.statusColor) } : {}">
                <span>{{ t('invoice.approval.step') }}: {{ s.approvalLevel }}</span>
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
    .timeline { display: flex; align-items: stretch; gap: 16px; overflow-x: auto; padding: 8px 0; flex-wrap: wrap; }
    .timeline-step {
      display: flex; align-items: center; position: relative; border: 2px solid; border-radius: 12px; padding: 12px 16px; flex: 1; min-width: 250px; transition: all 0.2s ease;
    }
    .step-number {
      width: 36px; height: 36px; border-radius: 50%; background: rgba(255,255,255,0.3); color: #fff; display: flex; align-items: center; justify-content: center; font-weight: 700; margin-right: 12px; font-size: 16px; flex-shrink: 0;
    }
    .step-content { min-width: 180px; }
    .step-header { display: flex; align-items: center; gap: 8px; margin-bottom: 4px; flex-wrap: wrap; }
    .status-label { font-weight: 700; font-size: 14px; }
    .approver { font-weight: 500; font-size: 13px; }
    .step-meta { font-size: 12px; margin-top: 4px; }
    .connector { display: flex; align-items: center; }
    .connector .line { width: 24px; height: 2px; background: #ddd; margin-left: 8px; margin-right: 8px; }

    .timeline-error { display: flex; align-items: center; gap: 8px; color: #d32f2f; padding: 8px 0; }
  `]
})
export class ApprovalTimelineComponent implements OnInit, OnChanges {
  @Input() invoiceId!: number;
  @Input() workflows: ApprovalWorkflow[] | null = null;

  loading = false;
  steps: ApprovalWorkflow[] = [];
  error: string | null = null;

  constructor(
    private approvalService: ApprovalService,
    private statusTranslator: StatusTranslatorService,
    private languageService: LanguageService
  ) {}

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

  translateStatus(status?: string): string {
    if (!status) return '';
    return this.statusTranslator.translate(status, 'ApprovalWorkflow');
  }

  getContrastColor(hexColor: string): string {
    // Determine if text should be white or black based on background brightness
    if (!hexColor) return '#000000';

    const hex = hexColor.replace('#', '');
    const r = parseInt(hex.substring(0, 2), 16);
    const g = parseInt(hex.substring(2, 4), 16);
    const b = parseInt(hex.substring(4, 6), 16);

    // Calculate luminance using relative luminance formula
    const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;

    return luminance > 0.5 ? '#000000' : '#FFFFFF';
  }

  t(key: string): string {
    return this.languageService.translateKey(key);
  }
}
