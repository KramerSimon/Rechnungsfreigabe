import { Pipe, PipeTransform, OnInit } from '@angular/core';
import { StatusService } from '../services/status.service';

@Pipe({
  name: 'statusDisplay',
  standalone: true
})
export class StatusDisplayPipe implements PipeTransform {
  private statusCache: Map<string, string> = new Map();

  constructor(private statusService: StatusService) {}

  transform(statusCode: string, entityType: string = 'Invoice'): string {
    if (!statusCode) return '';

    const cacheKey = `${entityType}:${statusCode}`;

    // Return cached value if available
    if (this.statusCache.has(cacheKey)) {
      return this.statusCache.get(cacheKey) || statusCode;
    }

    // Get from service (this will be synchronous after initial load)
    const displayName = this.statusService.getStatusDisplayName(statusCode, entityType);
    this.statusCache.set(cacheKey, displayName);

    return displayName || statusCode;
  }
}
