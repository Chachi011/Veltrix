import { Injectable, effect, signal } from '@angular/core';

const STORAGE_KEY = 'vx.sidebarCollapsed';

/** Sidebar collapsed / expanded, remembered between visits. */
@Injectable({ providedIn: 'root' })
export class LayoutService {
  readonly sidebarCollapsed = signal(readFlag());

  constructor() {
    effect(() => {
      const collapsed = this.sidebarCollapsed();
      try {
        localStorage.setItem(STORAGE_KEY, collapsed ? '1' : '0');
      } catch {
        // Storage blocked: the setting lasts for this visit only
      }
    });
  }

  toggleSidebar(): void {
    this.sidebarCollapsed.update((v) => !v);
  }
}

function readFlag(): boolean {
  try {
    return localStorage.getItem(STORAGE_KEY) === '1';
  } catch {
    return false;
  }
}
