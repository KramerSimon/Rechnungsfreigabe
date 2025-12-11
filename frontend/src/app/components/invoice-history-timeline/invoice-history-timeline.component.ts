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
          this.error = 'Fehler beim Laden der Rechnungshistorie. Lade Mock-Daten...';
          console.error('Error loading invoice history, loading mock data:', error);
          // Load mock data as fallback
          this.loadMockData();
          return of([]);
        }),
        finalize(() => this.isLoading = false)
      )
      .subscribe(timeline => {
        if (timeline && timeline.length > 0) {
          this.timeline = timeline;
        } else {
          // If no data returned, use mock data
          this.loadMockData();
        }
      });
  }

  private loadMockData() {
    // Mock timeline data for development/testing
    this.timeline = [
      {
        date: '2024-12-11',
        entries: [
          {
            id: 1,
            invoiceId: this.invoiceId,
            action: 'Rechnung eingereicht',
            actionType: 'Submitted',
            actionSource: 'User',
            changedByUser: {
              id: 1,
              username: 'mmustermann',
              firstName: 'Max',
              lastName: 'Mustermann',
              fullName: 'Max Mustermann'
            },
            changedAt: '2024-12-11T09:00:00Z',
            comments: 'Rechnung wurde vom Lieferanten eingereicht',
            displayIcon: 'description',
            displayColor: 'primary'
          },
          {
            id: 2,
            invoiceId: this.invoiceId,
            action: 'Kostenstelle zugewiesen',
            actionType: 'FieldChange',
            actionSource: 'User',
            changedByUser: {
              id: 2,
              username: 'aschmidt',
              firstName: 'Anna',
              lastName: 'Schmidt',
              fullName: 'Anna Schmidt'
            },
            changedAt: '2024-12-11T10:30:00Z',
            fieldChanges: {
              'costCenter': {
                oldValue: undefined,
                newValue: '4020 - IT'
              }
            },
            displayIcon: 'edit',
            displayColor: 'accent'
          },
          {
            id: 3,
            invoiceId: this.invoiceId,
            action: 'Automatische Eskalation',
            actionType: 'Escalation',
            actionSource: 'System',
            changedAt: '2024-12-11T14:00:00Z',
            comments: 'Rechnung automatisch an nächste Freigabeebene weitergeleitet',
            policyReference: 'POLICY_ESCALATION_24H',
            systemReason: '24h Freigabefrist überschritten',
            displayIcon: 'trending_up',
            displayColor: 'warn'
          }
        ]
      }
    ];
    this.isLoading = false;
    this.error = null;
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
