import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatBadgeModule } from '@angular/material/badge';
import { Router, RouterModule } from '@angular/router';
import { NotificationService } from '../../core/services/notification.service';
import { NotificationDto } from '../../core/models/notification.models';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule, MatListModule, MatIconModule, MatButtonModule, MatBadgeModule, RouterModule],
  templateUrl: './notifications.component.html',
  styleUrls: ['./notifications.component.scss']
})
export class NotificationsComponent implements OnInit {
  notifications: NotificationDto[] = [];
  loading = false;
  showUnreadOnly = true;

  constructor(private notificationService: NotificationService, private router: Router) {}

  ngOnInit(): void {
    this.loadNotifications();
  }

  loadNotifications(): void {
    this.loading = true;
    this.notificationService.getNotifications(this.showUnreadOnly).subscribe({
      next: (data) => {
        this.notifications = data;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  toggleUnreadOnly(): void {
    this.showUnreadOnly = !this.showUnreadOnly;
    this.loadNotifications();
  }

  markAsRead(notification: NotificationDto): void {
    if (notification.isRead) return;
    this.notificationService.markAsRead(notification.id).subscribe({
      next: () => {
        notification.isRead = true;
        // Reload to reflect updated unreadCount in polling
        setTimeout(() => this.loadNotifications(), 500);
      }
    });
  }

  markAllAsRead(): void {
    this.notificationService.markAllAsRead().subscribe({
      next: () => {
        // Reload to reflect updated unreadCount in polling
        setTimeout(() => this.loadNotifications(), 500);
      }
    });
  }

  navigateTo(notification: NotificationDto): void {
    // Mark as read when opening to keep badge in sync
    if (!notification.isRead) {
      this.notificationService.markAsRead(notification.id).subscribe();
      notification.isRead = true;
    }

    if (!notification.actionUrl) return;

    // Map backend actionUrl to frontend routes
    const url = notification.actionUrl.startsWith('/invoices/')
      ? notification.actionUrl.replace('/invoices/', '/invoice/')
      : notification.actionUrl;

    this.router.navigate([url]);
  }
}
