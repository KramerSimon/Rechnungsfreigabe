import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { finalize } from 'rxjs';
import { InvoiceService } from '../../../../../core/services/invoice.service';
import { LanguageService } from '../../../../../core/services/language.service';

@Component({
  selector: 'app-manual-workflow-action',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule],
  template: `
    <button
      *ngIf="canCreate"
      mat-stroked-button
      color="primary"
      class="workflow-action-btn"
      [disabled]="loading"
      (click)="onCreateWorkflow($event)"
    >
      <mat-icon>account_tree</mat-icon>
      {{ t('dashboard.accounting.workflow.create') }}
    </button>

    <span *ngIf="!canCreate" class="workflow-action-na">--</span>
  `,
  styles: [
    `
      .workflow-action-btn {
        min-width: 140px;
      }

      .workflow-action-na {
        color: var(--muted-text-color);
      }
    `
  ]
})
export class ManualWorkflowActionComponent {
  @Input() invoiceId = 0;
  @Input() canCreate = false;

  @Output() workflowCreated = new EventEmitter<void>();
  @Output() workflowCreateFailed = new EventEmitter<string>();

  loading = false;

  constructor(
    private invoiceService: InvoiceService,
    private languageService: LanguageService
  ) {}

  onCreateWorkflow(event: MouseEvent): void {
    event.stopPropagation();

    if (!this.canCreate || this.invoiceId <= 0 || this.loading) {
      return;
    }

    const confirmed = confirm(this.t('dashboard.accounting.workflow.confirmCreate'));
    if (!confirmed) {
      return;
    }

    this.loading = true;
    this.invoiceService.createManualWorkflow(this.invoiceId)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: () => this.workflowCreated.emit(),
        error: (error) => {
          const message = error?.error?.message || this.t('dashboard.accounting.workflow.error');
          this.workflowCreateFailed.emit(message);
        }
      });
  }

  t(key: string): string {
    return this.languageService.translateKey(key);
  }
}
