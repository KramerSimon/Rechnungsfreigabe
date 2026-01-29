import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Project } from '../../../../core/models/project.model';
import { CostCenter } from '../../../../core/models/cost-center.model';
import { User } from '../../../../core/models/user.models';
import { ProjectService } from '../../../../core/services/project.service';
import { CostCenterService } from '../../../../core/services/cost-center.service';
import { UserService } from '../../../../core/services/user.service';
import { CreateProjectDialogComponent } from './dialogs/create-project-dialog.component';
import { EditProjectDialogComponent } from './dialogs/edit-project-dialog/edit-project-dialog.component';
import { StatusDisplayPipe } from '../../../../core/pipes/status-display.pipe';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-projects-tab',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatSnackBarModule,
    MatCardModule,
    MatTooltipModule,
    StatusDisplayPipe,
  ],
  templateUrl: './projects-tab.component.html',
  styleUrls: ['./projects-tab.component.scss'],
})
export class ProjectsTabComponent implements OnInit {
  projects: Project[] = [];
  costCenters: CostCenter[] = [];
  users: User[] = [];
  loadingProjects = false;
  projectColumns = ['id', 'name', 'costCenter', 'budget', 'projectManager', 'status', 'actions'];

  constructor(
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private projectService: ProjectService,
    private costCenterService: CostCenterService,
    private userService: UserService
  ) {}

  ngOnInit(): void {
    this.loadProjects();
  }

  loadProjects(): void {
    this.loadingProjects = true;
    forkJoin({
      projects: this.projectService.getProjects(),
      costCenters: this.costCenterService.getCostCenters(),
      users: this.userService.getUsers()
    }).subscribe({
      next: (result) => {
        this.projects = result.projects;
        this.costCenters = result.costCenters;
        this.users = result.users;
        this.loadingProjects = false;
      },
      error: (error: any) => {
        console.error('Error loading projects:', error);
        this.loadingProjects = false;
        this.snackBar.open('Fehler beim Laden der Projekte', 'Schließen', {
          duration: 3000,
        });
      },
    });
  }

  createProject(): void {
    const dialogData = {
      project: {
        id: '',
        name: '',
        description: '',
        costCenterId: '',
        budget: 0,
        projectManagerId: undefined,
        status: 'active',
      } as Project,
      costCenters: this.costCenters,
      projectManagers: this.users.filter(u => this.isUserAManager(u))
    };

    const dialogRef = this.dialog.open(CreateProjectDialogComponent, {
      width: '600px',
      maxWidth: '95vw',
      data: dialogData,
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.projectService.addProject(result).subscribe({
          next: () => {
            this.snackBar.open('Projekt erfolgreich erstellt', 'Schließen', {
              duration: 3000,
            });
            this.loadProjects();
          },
          error: (error: any) => {
            console.error('Error creating project:', error);
            this.snackBar.open(
              'Fehler beim Erstellen des Projekts',
              'Schließen',
              { duration: 3000 }
            );
          },
        });
      }
    });
  }

  editProject(project: Project): void {
    const dialogData = {
      project: project,
      costCenters: this.costCenters,
      projectManagers: this.users.filter(u => this.isUserAManager(u))
    };

    const dialogRef = this.dialog.open(EditProjectDialogComponent, {
      width: '600px',
      maxWidth: '95vw',
      data: dialogData,
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.projectService.updateProject(project.id, result).subscribe({
          next: () => {
            this.snackBar.open(
              'Projekt erfolgreich aktualisiert',
              'Schließen',
              { duration: 3000 }
            );
            this.loadProjects();
          },
          error: (error: any) => {
            console.error('Error updating project:', error);
            this.snackBar.open(
              'Fehler beim Aktualisieren des Projekts',
              'Schließen',
              { duration: 3000 }
            );
          },
        });
      }
    });
  }

  deleteProject(id: string): void {
    if (confirm('Möchten Sie dieses Projekt wirklich löschen?')) {
      this.projectService.deleteProject(id).subscribe({
        next: () => {
          this.snackBar.open('Projekt erfolgreich gelöscht', 'Schließen', {
            duration: 3000,
          });
          this.loadProjects();
        },
        error: (error: any) => {
          console.error('Error deleting project:', error);
          this.snackBar.open(
            'Fehler beim Löschen des Projekts',
            'Schließen',
            { duration: 3000 }
          );
        },
      });
    }
  }

  getManagerName(project: Project): string {
    if (project.projectManager) {
      const fullName = [project.projectManager.firstName, project.projectManager.lastName]
        .filter(Boolean)
        .join(' ');
      return fullName || project.projectManager.username;
    }
    return '-';
  }

  getStatusClass(status: string): string {
    const statusLower = status?.toLowerCase().replace(/_/g, '_') || '';
    return `status-${statusLower}`;
  }

  getCostCenterName(costCenterId: string): string {
    const costCenter = this.costCenters.find(cc => cc.id === String(costCenterId));
    return costCenter ? costCenter.name : '-';
  }

  isUserAManager(user: User): boolean {
    const role = user.role?.toLowerCase();
    return role === 'manager' || role === 'admin' || role === 'administrator';
  }
}
