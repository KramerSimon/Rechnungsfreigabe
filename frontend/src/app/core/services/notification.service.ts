import { Injectable, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, interval, Subject } from 'rxjs';
import { switchMap, takeUntil, catchError, startWith, map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { NotificationDto, UnreadCountResponse } from '../models/notification.models';

@Injectable({ providedIn: 'root' })
export class NotificationService implements OnDestroy {
  private readonly baseUrl = `${environment.apiUrl}/notifications`;
  private readonly destroy$ = new Subject<void>();

  // Real-time unread count observable that polls every 30 seconds
  public readonly unreadCount$: Observable<number>;

  constructor(private http: HttpClient) {
    // Start with 0, then poll every 30 seconds and emit count
    this.unreadCount$ = interval(30000).pipe(
      startWith(0),
      switchMap(() => this.http.get<UnreadCountResponse>(`${this.baseUrl}/unread-count`)),
      map(response => response.count),
      catchError(() => {
        // On error, emit 0 and continue polling
        return new Observable<number>(subscriber => {
          subscriber.next(0);
          subscriber.complete();
        });
      }),
      takeUntil(this.destroy$)
    );
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  getNotifications(unreadOnly = false): Observable<NotificationDto[]> {
    const url = `${this.baseUrl}${unreadOnly ? '?unreadOnly=true' : ''}`;
    return this.http.get<NotificationDto[]>(url);
  }

  getUnreadCount(): Observable<UnreadCountResponse> {
    return this.http.get<UnreadCountResponse>(`${this.baseUrl}/unread-count`);
  }

  markAsRead(id: number): Observable<{ message: string }> {
    return this.http.patch<{ message: string }>(`${this.baseUrl}/${id}/read`, {});
  }

  markAllAsRead(): Observable<{ message: string }> {
    return this.http.patch<{ message: string }>(`${this.baseUrl}/read-all`, {});
  }
}
