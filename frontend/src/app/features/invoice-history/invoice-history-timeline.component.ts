import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatExpansionModule } from '@angular/material/expansion';

import { InvoiceHistoryService } from '../../core/services/invoice-history.service';
import {
  InvoiceHistoryTimelineDto,
  InvoiceHistoryDto,
  HistoryActionSource
} from '../../core/models/history.models';
import { catchError, finalize, of } from 'rxjs';

@Component({
  selector: 'app-invoice-history-timeline',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatDividerModule,
    MatTooltipModule,
    MatSnackBarModule,
    MatExpansionModule
  ],
  templateUrl: './invoice-history-timeline.component.html',
  styleUrl: './invoice-history-timeline.component.scss'
})
export class InvoiceHistoryTimelineComponent implements OnInit {
  @Input() invoiceId!: number;
  @Input() invoiceNumber: string = '';

  timeline: InvoiceHistoryTimelineDto[] = [];
  isLoading = false;
  error: string | null = null;

  constructor(
    private historyService: InvoiceHistoryService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit() {
    this.loadTimeline();
  }

  private loadTimeline() {
    this.isLoading = true;
    this.error = null;

    this.historyService.getInvoiceHistoryTimeline(this.invoiceId)
      .pipe(
        catchError(error => {
          this.error = 'Fehler beim Laden der Rechnungshistorie';
          console.error('Error loading invoice history:', error);
          this.timeline = [];
          return of([]);
        }),
        finalize(() => this.isLoading = false)
      )
      .subscribe(timeline => {
        this.timeline = timeline || [];
      });
  }

  getTimeForDisplay(entry: InvoiceHistoryDto): string {
    return this.historyService.formatTimeForDisplay(entry.changedAt);
  }

  getActionDescription(entry: InvoiceHistoryDto): string {
    return this.historyService.getActionDescription(entry.action, entry.actionType, entry.actionSource);
  }

  getUserDisplayName(entry: InvoiceHistoryDto): string {
    return this.historyService.getUserDisplayName(entry);
  }

  getContextualInfo(entry: InvoiceHistoryDto): string[] {
    return this.historyService.getContextualInfo(entry);
  }

  isSystemAction(entry: InvoiceHistoryDto): boolean {
    return entry.actionSource === HistoryActionSource.System ||
           entry.actionSource === HistoryActionSource.Policy ||
           entry.actionSource === HistoryActionSource.Escalation;
  }

  isEscalationEntry(entry: InvoiceHistoryDto): boolean {
    return this.historyService.isEscalationOrUrgent(entry);
  }

  getEntryIcon(entry: InvoiceHistoryDto): string {
    return entry.displayIcon;
  }

  getEntryColor(entry: InvoiceHistoryDto): string {
    return entry.displayColor;
  }

  hasFieldChanges(entry: InvoiceHistoryDto): boolean {
    return !!(entry.fieldChanges && Object.keys(entry.fieldChanges).length > 0);
  }

  getFieldChanges(entry: InvoiceHistoryDto): Array<{ field: string; oldValue: string; newValue: string; displayName: string }> {
    if (!entry.fieldChanges) return [];

    return Object.entries(entry.fieldChanges).map(([field, change]: [string, any]) => ({
      field,
      oldValue: change.OldValue || '(leer)',
      newValue: change.NewValue || '(leer)',
      displayName: change.DisplayName || field
    }));
  }

  onExportPdf() {
    this.historyService.exportHistoryAsPdf(this.invoiceId)
      .subscribe({
        next: (blob) => {
          const url = window.URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url;
          link.download = `Rechnung_${this.invoiceNumber}_Historie.pdf`;
          link.click();
          window.URL.revokeObjectURL(url);

          this.snackBar.open('Historie als PDF exportiert', 'Schließen', {
            duration: 3000,
            panelClass: ['success-snackbar']
          });
        },
        error: (error) => {
          console.error('Error exporting PDF:', error);
          this.snackBar.open('Fehler beim PDF-Export', 'Schließen', {
            duration: 5000,
            panelClass: ['error-snackbar']
          });
        }
      });
  }

  refresh() {
    this.loadTimeline();
  }
}
