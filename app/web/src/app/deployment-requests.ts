import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export type DeploymentEnvironment = 'staging' | 'production';

export const ENVIRONMENTS: readonly DeploymentEnvironment[] = ['staging', 'production'];

/** Limits mirrored from the API (DeploymentRequestRules). The API stays the source of truth. */
export const APPLICATION_MAX_LENGTH = 100;
export const VERSION_MAX_LENGTH = 50;

export interface DeploymentRequest {
  id: string;
  application: string;
  environment: DeploymentEnvironment;
  version: string;
  createdAt: string;
}

export interface CreateDeploymentRequest {
  application: string;
  environment: DeploymentEnvironment;
  version: string;
}

/** RFC 9457 problem details returned by the API for 400 / 409 / 500. */
export interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}

@Injectable({ providedIn: 'root' })
export class DeploymentRequestsApi {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/deployment-requests';

  list(): Observable<DeploymentRequest[]> {
    return this.http.get<DeploymentRequest[]>(this.url);
  }

  create(request: CreateDeploymentRequest): Observable<DeploymentRequest> {
    return this.http.post<DeploymentRequest>(this.url, request);
  }
}
