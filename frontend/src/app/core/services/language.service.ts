import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import {
  KEY_TRANSLATIONS,
  LANGUAGE_OPTIONS,
  LanguageCode,
  LanguageOption,
  PHRASE_TRANSLATIONS
} from '../i18n/translations';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly storageKey = 'app_language';
  private readonly languageSubject = new BehaviorSubject<LanguageCode>('de');
  readonly currentLanguage$ = this.languageSubject.asObservable();

  constructor() {
    const saved = localStorage.getItem(this.storageKey) as LanguageCode | null;
    if (saved && this.isSupported(saved)) {
      this.languageSubject.next(saved);
    }
  }

  get currentLanguage(): LanguageCode {
    return this.languageSubject.value;
  }

  get languageOptions(): LanguageOption[] {
    return LANGUAGE_OPTIONS;
  }

  setLanguage(language: LanguageCode): void {
    if (!this.isSupported(language) || language === this.currentLanguage) {
      return;
    }

    this.languageSubject.next(language);
    localStorage.setItem(this.storageKey, language);
  }

  translateKey(key: string): string {
    const lang = this.currentLanguage;
    return KEY_TRANSLATIONS[lang]?.[key] ?? KEY_TRANSLATIONS.de[key] ?? key;
  }

  translatePhrase(text: string): string {
    const normalized = this.normalize(text);
    const lang = this.currentLanguage;
    const translated = PHRASE_TRANSLATIONS[lang]?.[normalized];
    return translated ?? text;
  }

  private isSupported(language: string): language is LanguageCode {
    return LANGUAGE_OPTIONS.some((option) => option.code === language);
  }

  private normalize(value: string): string {
    return value
      .trim()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '');
  }
}
