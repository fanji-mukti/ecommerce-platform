import { Component, OnInit, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-callback',
  standalone: true,
  imports: [MatProgressSpinnerModule, MatButtonModule, RouterLink],
  templateUrl: './callback.component.html',
})
export class CallbackComponent implements OnInit {
  private oidcSecurityService = inject(OidcSecurityService);
  private router = inject(Router);

  hasError = signal<boolean>(false);

  ngOnInit(): void {
    this.oidcSecurityService.checkAuth().subscribe({
      next: (loginResponse) => {
        if (loginResponse.isAuthenticated) {
          this.router.navigate(['/catalog']);
        } else {
          this.hasError.set(true);
        }
      },
      error: () => {
        this.hasError.set(true);
      },
    });
  }
}
