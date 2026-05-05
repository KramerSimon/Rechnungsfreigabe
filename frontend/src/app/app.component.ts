import { Component, OnInit } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatBadgeModule } from '@angular/material/badge';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from './core/services/auth.service';
import { RoleService, UserRole, DashboardRoute } from './core/services/role.service';
import { UserService } from './core/services/user.service';
import { AuthState } from './core/models/auth.models';
import { Observable, map, take } from 'rxjs';
import { NotificationService } from './core/services/notification.service';
import { EditUserDialogComponent } from './features/master-data/tabs/users-tab/dialogs/edit-user-dialog/edit-user-dialog.component';
import { RoleDto } from './core/models/user.models';
import { LanguageService } from './core/services/language.service';
import { LanguageCode, LanguageOption } from './core/i18n/translations';

@Component({
  selector: 'app-root',
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatDividerModule,
    MatBadgeModule,
    MatTooltipModule
],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent implements OnInit {
  title = 'Rechnungsfreigabe';
  authState$: Observable<AuthState>;
  availableRoutes$: Observable<DashboardRoute[]>;
  currentRole$: Observable<string>;
  currentRoleTitle$: Observable<string>;
  unreadCount$: Observable<number>;
  isDarkMode = false;
  currentLanguage: LanguageCode;
  languageOptions: LanguageOption[];

  constructor(
    private authService: AuthService,
    private roleService: RoleService,
    private router: Router,
    private notificationService: NotificationService,
    private dialog: MatDialog,
    private userService: UserService,
    private snackBar: MatSnackBar,
    private languageService: LanguageService
  ) {
    this.authState$ = this.authService.authState$;
    this.availableRoutes$ = this.roleService.getAvailableNavigationRoutes();
    this.currentRole$ = this.roleService.getCurrentUserRole().pipe(
      map(role => role.toString())
    );
    this.currentRoleTitle$ = this.roleService.getCurrentDashboardTitle();
    // Use real-time polling unreadCount$ which refreshes every 30 seconds
    this.unreadCount$ = this.notificationService.unreadCount$;
    this.currentLanguage = this.languageService.currentLanguage;
    this.languageOptions = this.languageService.languageOptions;
  }

  ngOnInit(): void {
    this.initializeTheme();
    this.languageService.currentLanguage$.subscribe((language) => {
      this.currentLanguage = language;
    });
    // Navigation will be handled by auth guard and routing
  }

  setLanguage(language: LanguageCode): void {
    this.languageService.setLanguage(language);
  }

  t(key: string): string {
    return this.languageService.translateKey(key);
  }

  getLanguageLabel(code: LanguageCode): string {
    return this.t(`language.${code}`);
  }

  toggleDarkMode(): void {
    this.isDarkMode = !this.isDarkMode;
    localStorage.setItem('theme', this.isDarkMode ? 'dark' : 'light');
    this.applyTheme();
  }

  private initializeTheme(): void {
    const savedTheme = localStorage.getItem('theme');
    if (savedTheme === 'dark' || savedTheme === 'light') {
      this.isDarkMode = savedTheme === 'dark';
    } else {
      this.isDarkMode = window.matchMedia('(prefers-color-scheme: dark)').matches;
    }
    this.applyTheme();
  }

  private applyTheme(): void {
    document.body.classList.toggle('dark-theme', this.isDarkMode);
  }

  editCurrentUser(): void {
    this.authState$.pipe(take(1)).subscribe(authState => {
      if (!authState.user) return;

      // Get all available roles
      this.roleService.getRoles().subscribe({
        next: (roles: RoleDto[]) => {
          const dialogRef = this.dialog.open(EditUserDialogComponent, {
            width: '500px',
            maxWidth: '95vw',
            data: {
              user: authState.user,
              availableRoles: roles
            },
          });

          dialogRef.afterClosed().subscribe((result) => {
            if (result && authState.user) {
              this.userService.updateUser(authState.user.id.toString(), result).subscribe({
                next: () => {
                  this.snackBar.open(
                    'Benutzer erfolgreich aktualisiert',
                    'Schließen',
                    { duration: 3000 }
                  );
                  // Reload auth state to reflect changes
                  this.authService.refreshUserData();
                },
                error: (error) => {
                  console.error('Error updating user:', error);
                  this.snackBar.open(
                    'Fehler beim Aktualisieren des Benutzers',
                    'Schließen',
                    { duration: 3000 }
                  );
                },
              });
            }
          });
        },
        error: (error) => {
          console.error('Error loading roles:', error);
          this.snackBar.open(
            'Fehler beim Laden der Rollen',
            'Schließen',
            { duration: 3000 }
          );
        }
      });
    });
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
