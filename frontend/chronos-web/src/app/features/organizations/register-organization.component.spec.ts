import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { RegisterOrganizationComponent } from './register-organization.component';

describe('RegisterOrganizationComponent', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [RegisterOrganizationComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('registers an organization and shows its id on success', () => {
    const fixture = TestBed.createComponent(RegisterOrganizationComponent);
    fixture.detectChanges();

    const instance = fixture.componentInstance as unknown as {
      name: { set(v: string): void };
      code: { set(v: string): void };
    };
    instance.name.set('Acme Corp');
    instance.code.set('ACME');

    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));

    const req = httpMock.expectOne('/api/organizations');
    expect(req.request.body).toEqual({ name: 'Acme Corp', code: 'ACME' });
    req.flush('org-1');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.success').textContent).toContain('org-1');
  });

  it('shows an error message when registration fails', () => {
    const fixture = TestBed.createComponent(RegisterOrganizationComponent);
    fixture.detectChanges();

    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));

    const req = httpMock.expectOne('/api/organizations');
    req.flush('Organization code already in use.', { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.error').textContent).toContain(
      'Organization code already in use.'
    );
  });
});
