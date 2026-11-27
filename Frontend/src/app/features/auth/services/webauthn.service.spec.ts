import { TestBed } from '@angular/core/testing';
import { WebAuthnService } from './webauthn.service';
import { AuthService } from '../../../core/auth/auth.service';
import { firstValueFrom, of } from 'rxjs';
import { vi } from 'vitest';

describe('WebAuthnService', () => {
  let service: WebAuthnService;
  let authServiceMock: any;

  beforeEach(() => {
    authServiceMock = {
      getPasskeyCreationOptions: vi.fn(),
      registerPasskey: vi.fn(),
      getPasskeyRequestOptions: vi.fn(),
      loginWithPasskey: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        WebAuthnService,
        { provide: AuthService, useValue: authServiceMock }
      ]
    });
    service = TestBed.inject(WebAuthnService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should convert base64url to buffer and back', () => {
    const original = 'SGVsbG8td29ybGQ'; // "Hello-world" in base64url
    const buffer = service.base64urlToBuffer(original);
    const converted = service.bufferToBase64url(buffer.buffer as ArrayBuffer);
    expect(converted).toBe(original.replace(/-/g, '').replace(/_/g, '')); 
  });

  describe('passkey autofill', () => {
    const originalCredential = (globalThis as any).PublicKeyCredential;
    const originalCredentials = Object.getOwnPropertyDescriptor(navigator, 'credentials');

    afterEach(() => {
      (globalThis as any).PublicKeyCredential = originalCredential;
      if (originalCredentials) {
        Object.defineProperty(navigator, 'credentials', originalCredentials);
      } else {
        delete (navigator as any).credentials;
      }
    });

    it('is unavailable without WebAuthn', async () => {
      (globalThis as any).PublicKeyCredential = undefined;
      await expect(service.isAutofillAvailable()).resolves.toBe(false);
    });

    it('asks the browser whether conditional mediation is available', async () => {
      (globalThis as any).PublicKeyCredential = { isConditionalMediationAvailable: () => Promise.resolve(true) };
      await expect(service.isAutofillAvailable()).resolves.toBe(true);
    });

    it('requests a passkey for any user with conditional mediation and posts the assertion', async () => {
      const buffer = (text: string) => new TextEncoder().encode(text).buffer;
      const get = vi.fn().mockResolvedValue({
        id: 'cred',
        type: 'public-key',
        getClientExtensionResults: () => ({}),
        authenticatorAttachment: 'platform',
        response: { authenticatorData: buffer('a'), clientDataJSON: buffer('c'), signature: buffer('s'), userHandle: null },
      });
      Object.defineProperty(navigator, 'credentials', { value: { get }, configurable: true });
      authServiceMock.getPasskeyRequestOptions.mockReturnValue(of({ challenge: 'AQ', rpId: 'localhost' }));
      authServiceMock.loginWithPasskey.mockReturnValue(of({ accessToken: 'a' }));
      const controller = new AbortController();

      await firstValueFrom(service.loginWithAutofill(controller.signal));

      expect(authServiceMock.getPasskeyRequestOptions).toHaveBeenCalledWith(undefined);
      expect(get).toHaveBeenCalledWith(expect.objectContaining({ mediation: 'conditional', signal: controller.signal }));
      expect(authServiceMock.loginWithPasskey).toHaveBeenCalledWith(expect.objectContaining({ id: 'cred' }));
    });
  });
});
