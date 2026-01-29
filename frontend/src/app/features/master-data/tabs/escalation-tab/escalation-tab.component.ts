import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { EscalationRuleService } from '../../../../core/services/escalation-rule.service';
import { EscalationRule } from '../../../../core/models/escalation-rule.model';
import { EscalationRuleDialogComponent, EscalationRuleDialogData } from './dialogs/escalation-rule-dialog.component';
import { StatusDisplayPipe } from '../../../../core/pipes/status-display.pipe';
import { StatusService } from '../../../../core/services/status.service';
import { RoleService } from '../../../../core/services/role.service';
import { UserService } from '../../../../core/services/user.service';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-escalation-tab',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatSnackBarModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatChipsModule,
    StatusDisplayPipe
  ],
  templateUrl: './escalation-tab.component.html',
  styleUrls: ['./escalation-tab.component.scss']
})
export class EscalationTabComponent implements OnInit {
  displayedColumns: string[] = [
    'id',
    'name',
    'triggerStatuses',
    'triggerAfterHours',
    'repeatIntervalHours',
    'maxEscalations',
    'notifyRoles',
    'notifyUsers',
    'isActive',
    'actions'
  ];
  escalationRules: EscalationRule[] = [];
  isLoading = false;

  constructor(
    private escalationRuleService: EscalationRuleService,
    private statusService: StatusService,
    private roleService: RoleService,
    private userService: UserService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadEscalationRules();
  }

  loadEscalationRules(): void {
    this.isLoading = true;
    this.escalationRuleService.getRules().subscribe({
      next: (rules) => {
        this.escalationRules = rules;
        this.isLoading = false;
      },
      error: (error: any) => {
        console.error('Error loading escalation rules:', error);
        this.snackBar.open('Failed to load escalation rules', 'Close', { duration: 3000 });
        this.isLoading = false;
      }
    });
  }

  createEscalationRule(): void {
    forkJoin({
      statuses: this.statusService.getInvoiceStatuses(),
      roles: this.roleService.getRoles(),
      users: this.userService.getUsers()
    }).subscribe({
      next: (result) => {
        const dialogRef = this.dialog.open(EscalationRuleDialogComponent, {
          width: '700px',
          data: {
            mode: 'create',
            statuses: result.statuses,
            roles: result.roles,
            users: result.users
          } as EscalationRuleDialogData
        });

        dialogRef.afterClosed().subscribe(dialogResult => {
          if (dialogResult) {
            this.escalationRuleService.createRule(dialogResult).subscribe({
              next: () => {
                this.snackBar.open('Escalation rule created successfully', 'Close', { duration: 3000 });
                this.loadEscalationRules();
              },
              error: (error: any) => {
                console.error('Error creating escalation rule:', error);
                this.snackBar.open('Failed to create escalation rule', 'Close', { duration: 3000 });
              }
            });
          }
        });
      },
      error: (error: any) => {
        console.error('Error loading dialog data:', error);
        this.snackBar.open('Failed to load required data', 'Close', { duration: 3000 });
      }
    });
  }

  editEscalationRule(rule: EscalationRule): void {
    forkJoin({
      statuses: this.statusService.getInvoiceStatuses(),
      roles: this.roleService.getRoles(),
      users: this.userService.getUsers()
    }).subscribe({
      next: (result) => {
        const dialogRef = this.dialog.open(EscalationRuleDialogComponent, {
          width: '700px',
          data: {
            mode: 'edit',
            rule: { ...rule },
            statuses: result.statuses,
            roles: result.roles,
            users: result.users
          } as EscalationRuleDialogData
        });

        dialogRef.afterClosed().subscribe(dialogResult => {
          if (dialogResult) {
            this.escalationRuleService.updateRule(rule.id, dialogResult).subscribe({
              next: () => {
                this.snackBar.open('Escalation rule updated successfully', 'Close', { duration: 3000 });
                this.loadEscalationRules();
              },
              error: (error: any) => {
                console.error('Error updating escalation rule:', error);
                this.snackBar.open('Failed to update escalation rule', 'Close', { duration: 3000 });
              }
            });
          }
        });
      },
      error: (error: any) => {
        console.error('Error loading dialog data:', error);
        this.snackBar.open('Failed to load required data', 'Close', { duration: 3000 });
      }
    });
  }

  deleteEscalationRule(rule: EscalationRule): void {
    if (confirm(`Are you sure you want to delete the escalation rule "${rule.name}"?`)) {
      this.escalationRuleService.deleteRule(rule.id).subscribe({
        next: () => {
          this.snackBar.open('Escalation rule deleted successfully', 'Close', { duration: 3000 });
          this.loadEscalationRules();
        },
        error: (error: any) => {
          console.error('Error deleting escalation rule:', error);
          this.snackBar.open('Failed to delete escalation rule', 'Close', { duration: 3000 });
        }
      });
    }
  }

  formatMinutesToHoursAndMinutes(minutes: number): string {
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    if (hours > 0 && mins > 0) {
      return `${hours}h ${mins}m`;
    } else if (hours > 0) {
      return `${hours}h`;
    } else {
      return `${mins}m`;
    }
  }
}
