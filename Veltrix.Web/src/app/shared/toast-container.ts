import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ToastStore } from '../core/ui/toast.store';

@Component({
  selector: 'vx-toast-container',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="toasts" aria-live="polite">
      @for (toast of store.toasts(); track toast.id) {
        <div class="toast" [class]="'toast toast-' + toast.kind" role="status">
          <span>{{ toast.message }}</span>
          <button type="button" class="close" aria-label="Dismiss" (click)="store.dismiss(toast.id)">×</button>
        </div>
      }
    </div>
  `,
  styles: `
    .toasts {
      position: fixed;
      right: 16px;
      bottom: 16px;
      z-index: 1000;
      display: flex;
      flex-direction: column;
      gap: 8px;
      max-width: min(380px, calc(100vw - 32px));
    }
    .toast {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      padding: 12px 14px;
      border-radius: var(--radius-sm);
      background: var(--surface);
      border: 1px solid var(--border);
      border-left: 4px solid var(--primary);
      box-shadow: var(--shadow);
      font-size: 0.9rem;
      animation: slide-in 0.2s ease-out;
    }
    .toast-success { border-left-color: var(--success); }
    .toast-error { border-left-color: var(--danger); }
    .close {
      margin-left: auto;
      border: 0;
      background: none;
      color: var(--text-muted);
      font-size: 1.2rem;
      line-height: 1;
      cursor: pointer;
    }
    @keyframes slide-in {
      from { transform: translateY(8px); opacity: 0; }
      to { transform: none; opacity: 1; }
    }
  `
})
export class ToastContainer {
  protected readonly store = inject(ToastStore);
}
