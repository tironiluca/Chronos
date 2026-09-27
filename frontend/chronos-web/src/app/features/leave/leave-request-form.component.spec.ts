import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { LeaveRequestFormComponent } from './leave-request-form.component';

describe('LeaveRequestFormComponent', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [LeaveRequestFormComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('submits only type/startDate/endDate and emits the created id', () => {
    const fixture = TestBed.createComponent(LeaveRequestFormComponent);
    fixture.detectChanges();

    const instance = fixture.componentInstance as unknown as {
      startDate: { set(v: string): void };
      endDate: { set(v: string): void };
    };
    instance.startDate.set('2026-08-10');
    instance.endDate.set('2026-08-17');

    let emittedId: string | undefined;
    fixture.componentInstance.created.subscribe((id) => (emittedId = id));

    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));

    const req = httpMock.expectOne('/api/leave-requests');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      type: 'Vacation',
      startDate: '2026-08-10',
      endDate: '2026-08-17'
    });

    req.flush('generated-id');

    expect(emittedId).toBe('generated-id');
  });

  it('surfaces the server error and keeps the form usable', () => {
    const fixture = TestBed.createComponent(LeaveRequestFormComponent);
    fixture.detectChanges();

    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));

    const req = httpMock.expectOne('/api/leave-requests');
    req.flush('End date cannot precede start date.', { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.error').textContent).toContain('End date cannot precede start date.');
  });
});
