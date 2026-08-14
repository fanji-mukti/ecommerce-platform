import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [MatCardModule, MatButtonModule, RouterLink],
  templateUrl: './login.component.html',
})
export class LoginComponent {
  private oidcSecurityService = inject(OidcSecurityService);

  authorize(): void {
    this.oidcSecurityService.authorize();
  }
}
