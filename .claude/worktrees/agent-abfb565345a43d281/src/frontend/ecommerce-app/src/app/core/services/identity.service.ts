import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpResponse } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class IdentityService {
  private http = inject(HttpClient);

  // Security: POST body contains ONLY email and password — no other fields (T-02-11-01)
  register(email: string, password: string): Observable<HttpResponse<unknown>> {
    return this.http.post<unknown>('/api/identity/register', { email, password }, { observe: 'response' });
  }
}
