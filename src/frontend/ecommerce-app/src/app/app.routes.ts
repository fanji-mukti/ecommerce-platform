import { Routes } from '@angular/router';
import { CatalogListComponent } from './features/catalog/catalog-list/catalog-list.component';
import { ProductDetailComponent } from './features/catalog/product-detail/product-detail.component';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { CallbackComponent } from './features/auth/callback/callback.component';

export const routes: Routes = [
  { path: '', redirectTo: 'catalog', pathMatch: 'full' },
  { path: 'catalog', component: CatalogListComponent, title: 'Catalog — eCommerce' },
  { path: 'product/:id', component: ProductDetailComponent, title: 'Product — eCommerce' },
  { path: 'login', component: LoginComponent, title: 'Sign In — eCommerce' },
  { path: 'register', component: RegisterComponent, title: 'Create Account — eCommerce' },
  { path: 'callback', component: CallbackComponent, title: 'eCommerce' },
  { path: '**', redirectTo: 'catalog' },
];
