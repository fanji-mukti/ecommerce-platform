import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { OidcSecurityService } from 'angular-auth-oidc-client';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatButtonModule,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private oidcSecurityService = inject(OidcSecurityService);

  // Use the built-in signals from angular-auth-oidc-client
  readonly isAuthenticated = this.oidcSecurityService.authenticated;
  readonly userData = this.oidcSecurityService.userData;

  signOut(): void {
    this.oidcSecurityService.logoff().subscribe();
  }
}
