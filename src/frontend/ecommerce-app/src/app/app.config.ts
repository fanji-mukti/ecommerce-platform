import { ApplicationConfig, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideAuth, authInterceptor } from 'angular-auth-oidc-client';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    provideRouter(routes),
    provideHttpClient(withFetch(), withInterceptors([authInterceptor()])),
    provideAuth({
      config: {
        authority: 'http://localhost:5005',
        redirectUrl: 'http://localhost:4200/callback',
        postLogoutRedirectUri: 'http://localhost:4200',
        clientId: 'ecommerce-spa',
        scope: 'openid profile email',
        responseType: 'code',
        silentRenew: false,
        useRefreshToken: false,
        secureRoutes: ['http://localhost:4200/api'],
      },
    }),
  ],
};
