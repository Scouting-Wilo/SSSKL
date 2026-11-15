import { TestBed } from '@angular/core/testing';
import { Component } from '@angular/core';
import { Router, provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ConfirmationService, MessageService } from 'primeng/api';
import { MainLayoutComponent } from './main-layout.component';
import { EmbeddedService } from '../../core/services/embedded.service';

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
});
