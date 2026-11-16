import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { Observable, Subject, of, throwError } from 'rxjs';
import { describe, it, expect, vi } from 'vitest';
import LoginComponent from './login.component';
import { WebAuthnService } from './services/webauthn.service';
import { EmbeddedService } from '../../core/services/embedded.service';
import { TurnstileService } from '../../core/services/turnstile.service';

function setup(options: { available?: boolean; embedded?: boolean; autofill?: () => Observable<unknown> } = {}) {
  const signals: AbortSignal[] = [];
  const webAuthn = {
    isAutofillAvailable: vi.fn(() => Promise.resolve(options.available ?? true)),
    loginWithAutofill: vi.fn((signal: AbortSignal) => {
      signals.push(signal);
      return options.autofill ? options.autofill() : new Subject<unknown>();
    }),
    login: vi.fn(() => of({})),
  };
  TestBed.configureTestingModule({
    imports: [LoginComponent],
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      provideRouter([]),
      { provide: WebAuthnService, useValue: webAuthn },
      { provide: EmbeddedService, useValue: { isEmbedded: options.embedded ?? false, notifyNative: vi.fn() } },
    ],
  });
  vi.spyOn(TestBed.inject(TurnstileService), 'load').mockReturnValue(new Promise(() => {}));
  const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
  const fixture = TestBed.createComponent(LoginComponent);
  const component = fixture.componentInstance as any;
  return { fixture, component, webAuthn, signals, navigate };
}

describe('LoginComponent passkey autofill', () => {
  it('starts a conditional passkey request after rendering', async () => {
    const { fixture, webAuthn } = setup();

    fixture.detectChanges();
    await fixture.whenStable();

    expect(webAuthn.loginWithAutofill).toHaveBeenCalledTimes(1);
  });

  it('does nothing when the browser has no passkey autofill', async () => {
    const { component, webAuthn } = setup({ available: false });

    await component.startPasskeyAutofill();

    expect(webAuthn.loginWithAutofill).not.toHaveBeenCalled();
  });

  it('does nothing inside the mobile app', async () => {
    const { component, webAuthn } = setup({ embedded: true });

    await component.startPasskeyAutofill();

    expect(webAuthn.isAutofillAvailable).not.toHaveBeenCalled();
    expect(webAuthn.loginWithAutofill).not.toHaveBeenCalled();
  });

  it('keeps a single pending request', async () => {
    const { component, webAuthn } = setup();

    await component.startPasskeyAutofill();
    await component.startPasskeyAutofill();

    expect(webAuthn.loginWithAutofill).toHaveBeenCalledTimes(1);
  });

  it('navigates when the user picks a passkey from the autofill', async () => {
    const { component, navigate } = setup({ autofill: () => of({}) });

    await component.startPasskeyAutofill();

    expect(navigate).toHaveBeenCalledWith('/pos');
  });

  it('aborts the autofill request before the passkey button starts its own', async () => {
    const { component, webAuthn, signals } = setup();
    await component.startPasskeyAutofill();

    component.onPasskeyLogin();

    expect(signals[0].aborted).toBe(true);
    expect(webAuthn.login).toHaveBeenCalled();
  });

  it('aborts the autofill request before a password login', async () => {
    const { component, signals } = setup();
    await component.startPasskeyAutofill();
    component.loginForm.patchValue({ userName: 'kim', password: 'secret1' });
    component.turnstileToken.set('tok');

    component.onSubmit();

    expect(signals[0].aborted).toBe(true);
  });

  it('aborts the autofill request when the page closes', async () => {
    const { fixture, component, signals } = setup();
    await component.startPasskeyAutofill();

    fixture.destroy();

    expect(signals[0].aborted).toBe(true);
  });

  it('shows a failed passkey and offers the autofill again, but gives up after repeated failures', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
    const { component, webAuthn } = setup({ autofill: () => throwError(() => new Error('Ongeldige passkey')) });

    await component.startPasskeyAutofill();
    await vi.waitFor(() => expect(webAuthn.loginWithAutofill).toHaveBeenCalledTimes(3));
    await new Promise(resolve => setTimeout(resolve));

    expect(component.errorMessage()).toContain('Ongeldige passkey');
    expect(webAuthn.loginWithAutofill).toHaveBeenCalledTimes(3);
  });
});
