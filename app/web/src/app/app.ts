import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  APPLICATION_MAX_LENGTH,
  DeploymentEnvironment,
  DeploymentRequest,
  DeploymentRequestsApi,
  ENVIRONMENTS,
  ProblemDetails,
  VERSION_MAX_LENGTH,
} from './deployment-requests';

@Component({
  selector: 'app-root',
  imports: [ReactiveFormsModule, DatePipe],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  private readonly api = inject(DeploymentRequestsApi);

  protected readonly environments = ENVIRONMENTS;
  protected readonly applicationMaxLength = APPLICATION_MAX_LENGTH;
  protected readonly versionMaxLength = VERSION_MAX_LENGTH;

  protected readonly form = inject(NonNullableFormBuilder).group({
    application: ['', [Validators.required, Validators.maxLength(APPLICATION_MAX_LENGTH)]],
    environment: ['staging' as DeploymentEnvironment, Validators.required],
    version: ['', [Validators.required, Validators.maxLength(VERSION_MAX_LENGTH)]],
  });

  protected readonly requests = signal<DeploymentRequest[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly fieldErrors = signal<Partial<Record<string, string[]>>>({});
  protected readonly successMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.loadRequests();
  }

  protected loadRequests(): void {
    this.loading.set(true);
    this.loadError.set(null);
    this.api.list().subscribe({
      next: (requests) => {
        this.requests.set(requests);
        this.loading.set(false);
      },
      error: () => {
        this.loadError.set('Impossible de charger les demandes. Réessayez dans quelques instants.');
        this.loading.set(false);
      },
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.fieldErrors.set({});
    this.successMessage.set(null);

    // The form keeps its values after submission: submitting again shows the duplicate rule (409).
    this.api.create(this.form.getRawValue()).subscribe({
      next: (created) => {
        this.requests.update((requests) => [created, ...requests]);
        this.successMessage.set(
          `Demande enregistrée : ${created.application} (${created.environment}) en version ${created.version}.`,
        );
        this.submitting.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.handleError(error);
        this.submitting.set(false);
      },
    });
  }

  protected isInvalid(field: 'application' | 'environment' | 'version'): boolean {
    const control = this.form.controls[field];
    return control.invalid && control.touched;
  }

  private handleError(error: HttpErrorResponse): void {
    const problem = (error.error ?? {}) as ProblemDetails;

    if (error.status === 409) {
      this.errorMessage.set(problem.detail ?? 'Cette demande existe déjà.');
    } else if (error.status === 400) {
      this.errorMessage.set(problem.title ?? 'La demande est invalide.');
      this.fieldErrors.set(problem.errors ?? {});
    } else if (error.status === 0) {
      this.errorMessage.set('Le serveur est injoignable. Vérifiez votre connexion et réessayez.');
    } else {
      this.errorMessage.set('Une erreur inattendue est survenue. Réessayez dans quelques instants.');
    }
  }
}
