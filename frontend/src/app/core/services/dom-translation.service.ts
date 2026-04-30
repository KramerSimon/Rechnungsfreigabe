import { Injectable, NgZone } from '@angular/core';
import { Subscription } from 'rxjs';
import { LanguageService } from './language.service';

@Injectable({ providedIn: 'root' })
export class DomTranslationService {
  private observer?: MutationObserver;
  private subscription?: Subscription;
  private isApplyingTranslations = false;
  private readonly textNodeOriginal = new WeakMap<Text, string>();
  private readonly elementOriginal = new WeakMap<Element, Record<string, string>>();

  constructor(
    private languageService: LanguageService,
    private ngZone: NgZone
  ) {}

  start(): void {
    if (this.subscription) {
      return;
    }

    this.subscription = this.languageService.currentLanguage$.subscribe(() => {
      this.translateDocument();
    });

    this.ngZone.runOutsideAngular(() => {
      this.observer = new MutationObserver((mutations) => {
        if (this.isApplyingTranslations) {
          return;
        }

        for (const mutation of mutations) {
          if (mutation.type === 'characterData' && mutation.target instanceof Text) {
            this.translateTextNode(mutation.target);
          }

          mutation.addedNodes.forEach((node) => {
            this.translateNode(node);
          });
        }
      });

      this.observer.observe(document.body, {
        childList: true,
        subtree: true,
        characterData: true
      });
    });

    this.translateDocument();
  }

  private translateDocument(): void {
    if (!document?.body) {
      return;
    }

    this.isApplyingTranslations = true;
    try {
      this.translateNode(document.body);
    } finally {
      this.isApplyingTranslations = false;
    }
  }

  private translateNode(node: Node): void {
    if (node.nodeType === Node.TEXT_NODE) {
      this.translateTextNode(node as Text);
      return;
    }

    if (node.nodeType !== Node.ELEMENT_NODE) {
      return;
    }

    const element = node as Element;
    this.translateElementAttributes(element);

    for (const child of Array.from(element.childNodes)) {
      this.translateNode(child);
    }
  }

  private translateTextNode(node: Text): void {
    const parent = node.parentElement;
    if (!parent || this.isExcluded(parent)) {
      return;
    }

    const raw = node.textContent ?? '';
    if (!raw.trim()) {
      return;
    }

    if (!this.textNodeOriginal.has(node)) {
      this.textNodeOriginal.set(node, raw);
    }

    const original = this.textNodeOriginal.get(node) ?? raw;
    const match = original.match(/^(\s*)(.*?)(\s*)$/s);
    if (!match) {
      return;
    }

    const translated = this.languageService.translatePhrase(match[2]);
    const next = `${match[1]}${translated}${match[3]}`;
    if (node.textContent !== next) {
      node.textContent = next;
    }
  }

  private translateElementAttributes(element: Element): void {
    const attributes = ['placeholder', 'title', 'aria-label'];
    let originalAttrs = this.elementOriginal.get(element);

    if (!originalAttrs) {
      originalAttrs = {};
      for (const name of attributes) {
        const value = element.getAttribute(name);
        if (value) {
          originalAttrs[name] = value;
        }
      }
      this.elementOriginal.set(element, originalAttrs);
    }

    for (const name of attributes) {
      const original = originalAttrs[name];
      if (!original) {
        continue;
      }

      const translated = this.languageService.translatePhrase(original);
      if (element.getAttribute(name) !== translated) {
        element.setAttribute(name, translated);
      }
    }
  }

  private isExcluded(element: Element): boolean {
    const tag = element.tagName;
    return tag === 'SCRIPT'
      || tag === 'STYLE'
      || tag === 'CODE'
      || tag === 'PRE'
      || tag === 'TEXTAREA'
      || tag === 'MAT-ICON';
  }
}
