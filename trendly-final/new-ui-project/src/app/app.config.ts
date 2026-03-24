import { ApplicationConfig, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors, withFetch } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { routes } from './app.routes';
import { appInterceptor } from './core/interceptors/app.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    // withComponentInputBinding allows route params to bind directly to @Input() in components
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([appInterceptor]), withFetch()),
    provideAnimations(),
  ]
};
