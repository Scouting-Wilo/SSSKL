import { TestBed } from '@angular/core/testing';
import { Component, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { SidebarComponent } from './sidebar.component';
import { AuthService } from '../../core/auth/auth.service';
import { UserProfileDrawerService } from '../../core/services/user-profile-drawer.service';
import { EmbeddedService } from '../../core/services/embedded.service';

@Component({ template: '' })
class Blank {}

describe('SidebarComponent (render)', () => {
  let currentUser: ReturnType<typeof signal<any>>;
  let logout: ReturnType<typeof vi.fn>;

  function render() {
    TestBed.configureTestingModule({
      imports: [SidebarComponent, NoopAnimationsModule],
      providers: [
        provideRouter([{ path: '**', component: Blank }]),
        {
          provide: AuthService,
          useValue: {
            currentUser,
            isInitialized: signal(true),
            isAuthenticated: () => !!currentUser(),
            logout,
          },
        },
        { provide: HttpClient, useValue: { get: vi.fn(() => of({ items: [] })) } },
        { provide: UserProfileDrawerService, useValue: { open: vi.fn() } },
        { provide: EmbeddedService, useValue: { isEmbedded: false } },
      ],
    });
    const fixture = TestBed.createComponent(SidebarComponent);
    fixture.componentRef.setInput('isOpen', true);
    const closed = vi.fn();
    fixture.componentInstance.closed.subscribe(closed);
    fixture.detectChanges();
    return { fixture, root: fixture.nativeElement as HTMLElement, closed };
  }

  beforeEach(() => {
    currentUser = signal<any>(null);
    logout = vi.fn();
  });

  it('closes when the overlay, the menu or a sign-in link is clicked', async () => {
    const { fixture, root, closed } = render();

    (root.querySelector('.mobile-overlay') as HTMLElement).click();
    (root.querySelector('p-menu') as HTMLElement).click();
    const links = Array.from(root.querySelectorAll<HTMLAnchorElement>('aside a[href]')).filter(
      (a) => a.getAttribute('href')?.startsWith('/login') || a.getAttribute('href') === '/register',
    );
    expect(links).toHaveLength(2);
    links.forEach((link) => link.click());
    await fixture.whenStable();

    expect(closed).toHaveBeenCalledTimes(4);
  });

  it('signs out and closes from the logout link', () => {
    currentUser.set({ id: 'u1', fullName: 'Jan', roles: ['User'] });
    const { root, closed } = render();

    (root.querySelector('a.text-red-500') as HTMLElement).click();

    expect(logout).toHaveBeenCalled();
    expect(closed).toHaveBeenCalledTimes(1);
  });
});
