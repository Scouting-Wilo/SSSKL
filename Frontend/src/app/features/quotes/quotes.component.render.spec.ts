import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { By } from '@angular/platform-browser';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MessageService } from 'primeng/api';
import { of } from 'rxjs';
import { vi } from 'vitest';
import QuotesComponent from './quotes.component';
import { QuoteService } from './services/quote.service';
import { AuthService } from '../../core/auth/auth.service';
import { GlobalSettingsService } from '../admin/services/global-settings.service';
import { EmbeddedService } from '../../core/services/embedded.service';
import { UserProfileDrawerService } from '../../core/services/user-profile-drawer.service';
import { QuoteCardComponent } from './components/quote-card/quote-card.component';

describe('QuotesComponent (render)', () => {
  const quote = {
    id: 'q1',
    text: 'Dat is geen bug',
    dateSaid: '2026-01-01T00:00:00Z',
    hasVoted: false,
    voteCount: 0,
    commentsCount: 0,
    createdBy: { id: 'u1' },
    authors: [{ id: 'a1', applicationUserId: null, customName: 'Oom Kees' }],
  } as any;

  it('wires the quote card events to the page', async () => {
    const getQuotes = vi.fn(() => of({ items: [quote], totalCount: 1 }));
    TestBed.configureTestingModule({
      imports: [QuotesComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        MessageService,
        { provide: QuoteService, useValue: { getQuotes, toggleVote: vi.fn(() => of(true)) } },
        { provide: AuthService, useValue: { currentUser: signal({ id: 'u1', roles: ['Admin'] }) } },
        { provide: GlobalSettingsService, useValue: { getSetting: vi.fn(() => of(null)) } },
        { provide: EmbeddedService, useValue: { isEmbedded: false, notifyNative: vi.fn() } },
        { provide: UserProfileDrawerService, useValue: { open: vi.fn() } },
      ],
    });
    const fixture = TestBed.createComponent(QuotesComponent);
    const component = fixture.componentInstance;
    const openForm = vi.spyOn(component, 'openQuoteForm').mockImplementation(() => undefined);
    const confirmDelete = vi.spyOn(component, 'confirmDelete').mockImplementation(() => undefined);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const card = fixture.debugElement.query(By.directive(QuoteCardComponent));
    expect(card).toBeTruthy();
    getQuotes.mockClear();

    card.triggerEventHandler('edit', quote);
    card.triggerEventHandler('remove', quote);
    card.triggerEventHandler('voted', quote);

    expect(openForm).toHaveBeenCalledWith(quote);
    expect(confirmDelete).toHaveBeenCalledWith(quote);
    expect(getQuotes).toHaveBeenCalled();
  });
});
