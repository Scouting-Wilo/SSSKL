import { TestBed } from '@angular/core/testing';
import { Component } from '@angular/core';
import { Router, provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ConfirmationService, MessageService } from 'primeng/api';
import { MainLayoutComponent } from './main-layout.component';
import { EmbeddedService } from '../../core/services/embedded.service';
import { signal } from '@angular/core';
import { By } from '@angular/platform-browser';
import { HttpClient } from '@angular/common/http';
import { of } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { UserProfileDrawerService } from '../../core/services/user-profile-drawer.service';
import { SidebarComponent } from '../sidebar/sidebar.component';

describe('MainLayoutComponent (render, mobile app)', () => {
  it('leaves the header and menu to the app', () => {
    TestBed.configureTestingModule({
      imports: [MainLayoutComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        MessageService,
        ConfirmationService, { provide: EmbeddedService, useValue: { isEmbedded: true } }],
    });
    const fixture = TestBed.createComponent(MainLayoutComponent);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;

    expect(root.querySelector('app-header')).toBeNull();
    expect(root.querySelector('app-sidebar')).toBeNull();
    expect(root.querySelector('.layout-container')?.classList).toContain('embedded');
    expect(root.querySelector('main.content-area')).not.toBeNull();
  });

  it('closes an open confirmation when navigating to another page', async () => {
    @Component({ template: '' })
    class Blank {}
    TestBed.configureTestingModule({
      imports: [MainLayoutComponent, NoopAnimationsModule],
      providers: [
        provideRouter([{ path: 'other', component: Blank }]),
        MessageService,
        ConfirmationService, { provide: EmbeddedService, useValue: { isEmbedded: true } }],
    });
    const fixture = TestBed.createComponent(MainLayoutComponent);
    fixture.detectChanges();
    const close = vi.spyOn(TestBed.inject(ConfirmationService), 'close');

    await TestBed.inject(Router).navigateByUrl('/other');

    expect(close).toHaveBeenCalled();
  });

  it('closes the menu when the sidebar asks to', () => {
    TestBed.configureTestingModule({
      imports: [MainLayoutComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        MessageService,
        ConfirmationService,
        { provide: EmbeddedService, useValue: { isEmbedded: false } },
        {
          provide: AuthService,
          useValue: { currentUser: signal(null), isInitialized: signal(true), isAuthenticated: () => false },
        },
        { provide: HttpClient, useValue: { get: vi.fn(() => of({ items: [] })) } },
        { provide: NotificationService, useValue: { unreadCount: signal(0), fetchUnreadCount: vi.fn(() => of(0)) } },
        { provide: UserProfileDrawerService, useValue: { open: vi.fn() } },
      ],
    });
    const fixture = TestBed.createComponent(MainLayoutComponent);
    fixture.componentInstance.isSidebarOpen.set(true);
    fixture.detectChanges();

    fixture.debugElement.query(By.directive(SidebarComponent)).triggerEventHandler('closed');

    expect(fixture.componentInstance.isSidebarOpen()).toBe(false);
  });
});
