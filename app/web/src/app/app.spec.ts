import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { App } from './app';
import { DeploymentRequest } from './deployment-requests';

const URL = '/api/deployment-requests';

const existing: DeploymentRequest = {
  id: '1',
  application: 'Billing-API',
  environment: 'staging',
  version: '1.2.0',
  createdAt: '2026-11-22T10:00:00Z',
};

describe('App', () => {
  let fixture: ComponentFixture<App>;
  let http: HttpTestingController;
  let element: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(App);
    http = TestBed.inject(HttpTestingController);
    element = fixture.nativeElement as HTMLElement;
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  async function render(): Promise<void> {
    fixture.detectChanges();
    await fixture.whenStable();
  }

  function fillAndSubmit(application: string, environment: string, version: string): void {
    const set = (selector: string, value: string) => {
      const input = element.querySelector<HTMLInputElement | HTMLSelectElement>(selector)!;
      input.value = value;
      input.dispatchEvent(new Event(input.tagName === 'SELECT' ? 'change' : 'input'));
    };
    set('input[name=application]', application);
    set('select[name=environment]', environment);
    set('input[name=version]', version);
    element.querySelector<HTMLFormElement>('form')!.dispatchEvent(new Event('submit'));
  }

  it('loads and displays the existing requests', async () => {
    http.expectOne(URL).flush([existing]);
    await render();

    const rows = element.querySelectorAll('[data-testid=request-row]');
    expect(rows.length).toBe(1);
    expect(rows[0].textContent).toContain('Billing-API');
    expect(rows[0].textContent).toContain('1.2.0');
  });

  it('shows an empty state when there is no request', async () => {
    http.expectOne(URL).flush([]);
    await render();

    expect(element.querySelector('[data-testid=empty]')).not.toBeNull();
  });

  it('creates a request and adds it at the top of the list', async () => {
    http.expectOne(URL).flush([existing]);
    await render();

    fillAndSubmit('Orders', 'production', '2.0.0');
    const request = http.expectOne({ method: 'POST', url: URL });
    expect(request.request.body).toEqual({ application: 'Orders', environment: 'production', version: '2.0.0' });
    request.flush({ ...existing, id: '2', application: 'Orders', environment: 'production', version: '2.0.0' }, { status: 201, statusText: 'Created' });
    await render();

    const rows = element.querySelectorAll('[data-testid=request-row]');
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('Orders');
    expect(element.querySelector('[data-testid=success]')?.textContent).toContain('Demande enregistrée');
  });

  it('displays the API message when the request is a duplicate (409)', async () => {
    http.expectOne(URL).flush([existing]);
    await render();

    fillAndSubmit('billing-api', 'staging', '1.2.0');
    const detail = "Une demande existe déjà pour l'application « billing-api », l'environnement « staging » et la version « 1.2.0 ».";
    http.expectOne({ method: 'POST', url: URL }).flush(
      { status: 409, title: 'Demande déjà enregistrée', detail },
      { status: 409, statusText: 'Conflict' },
    );
    await render();

    expect(element.querySelector('[data-testid=error]')?.textContent).toContain(detail);
    expect(element.querySelectorAll('[data-testid=request-row]').length).toBe(1);
  });

  it('displays field errors returned by the API (400)', async () => {
    http.expectOne(URL).flush([]);
    await render();

    fillAndSubmit('app', 'staging', '1.0.0');
    http.expectOne({ method: 'POST', url: URL }).flush(
      { status: 400, title: 'La demande est invalide.', errors: { version: ['La version ne doit pas dépasser 50 caractères.'] } },
      { status: 400, statusText: 'Bad Request' },
    );
    await render();

    expect(element.querySelector('[data-testid=error]')?.textContent).toContain('La demande est invalide.');
    expect(element.textContent).toContain('La version ne doit pas dépasser 50 caractères.');
  });

  it('does not call the API when required fields are empty', async () => {
    http.expectOne(URL).flush([]);
    await render();

    fillAndSubmit('', 'staging', '');
    await render();

    http.expectNone({ method: 'POST', url: URL });
    expect(element.textContent).toContain("L'application est obligatoire.");
  });
});
