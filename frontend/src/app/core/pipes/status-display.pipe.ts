import { Pipe, PipeTransform, OnInit } from '@angular/core';
import { StatusService } from '../services/status.service';
import { StatusTranslatorService } from '../services/status-translator.service';
import { LanguageService } from '../services/language.service';

@Pipe({
  name: 'statusDisplay',
  standalone: true,
  pure: false
})
export class StatusDisplayPipe implements PipeTransform {
  private statusCache: Map<string, string> = new Map();

  constructor(
    private statusService: StatusService,
    private statusTranslator: StatusTranslatorService,
    private languageService: LanguageService
  ) {}

  transform(statusCode: string, entityType: string = 'Invoice'): string {
    if (!statusCode) return '';

    const cacheKey = `${this.languageService.currentLanguage}:${entityType}:${statusCode}`;

    // Return cached value if available
    if (this.statusCache.has(cacheKey)) {
      return this.statusCache.get(cacheKey) || statusCode;
    }

    // Get from service (synchronous after initial load)
    const displayName = this.statusService.getStatusDisplayName(statusCode, entityType);

    // If status metadata is not loaded yet, fall back to translating the code directly.
    const translated = displayName === statusCode
      ? this.statusTranslator.translate(statusCode, entityType)
      : this.statusTranslator.translateDisplayName(displayName, entityType);

    // Avoid caching unresolved raw values so async metadata can update later.
    if (translated && translated !== statusCode) {
      this.statusCache.set(cacheKey, translated);
    }

    return translated || statusCode;
  }
}
