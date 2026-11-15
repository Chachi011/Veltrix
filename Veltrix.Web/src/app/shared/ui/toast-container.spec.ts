import { Component, provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ToastStore } from '../../core/ui/toast.store';
import { ModalService } from './modal';
import { ToastContainer } from './toast-container';

@Component({ selector: 'vx-test-drawer-content', template: '<p style="height: 100vh">Drawer</p>' })
class DrawerContent {}

/** True when the element is what the user sees (and can click) at its own centre, not something covering it. */
function isOnTop(element: Element): boolean {
  const box = element.getBoundingClientRect();
  return element.contains(document.elementFromPoint(box.x + box.width / 2, box.y + box.height / 2));
}

describe('ToastContainer', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideZonelessChangeDetection()] });
  });

  it('shows an error toast ABOVE an open drawer or dialog, so an error is never hidden behind it', async () => {
    const fixture = TestBed.createComponent(ToastContainer);
    const drawer = TestBed.inject(ModalService).openDrawer(DrawerContent, { ariaLabel: 'Drawer', width: '100vw' });
    const dialog = TestBed.inject(ModalService).open(DrawerContent, { ariaLabel: 'Dialog' });

    TestBed.inject(ToastStore).error('The assistant is not available right now.');
    await fixture.whenStable();

    const toast = (fixture.nativeElement as HTMLElement).querySelector('[role=alert]');
    expect(toast?.textContent).toContain('The assistant is not available right now.');
    expect(isOnTop(toast!)).withContext('the toast is covered by the overlay').toBeTrue();

    dialog.close();
    drawer.close();
  });
});
