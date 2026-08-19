import { Routes } from '@angular/router';
import { CatalogListComponent } from './features/catalog/catalog-list/catalog-list.component';
import { ProductDetailComponent } from './features/catalog/product-detail/product-detail.component';
import { CartPageComponent } from './features/cart/cart-page/cart-page.component';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { CallbackComponent } from './features/auth/callback/callback.component';
import { CheckoutPageComponent } from './features/checkout/checkout-page/checkout-page.component';
import { OrderDetailComponent } from './features/orders/order-detail/order-detail.component';
import { NotificationsPageComponent } from './features/notifications/notifications-page/notifications-page.component';

export const routes: Routes = [
  { path: '', redirectTo: 'catalog', pathMatch: 'full' },
  { path: 'catalog', component: CatalogListComponent, title: 'Catalog — eCommerce' },
  { path: 'product/:id', component: ProductDetailComponent, title: 'Product — eCommerce' },
  { path: 'cart', component: CartPageComponent, title: 'Cart — eCommerce' },
  { path: 'checkout', component: CheckoutPageComponent, title: 'Checkout — eCommerce' },
  { path: 'orders/:id', component: OrderDetailComponent, title: 'Order — eCommerce' },
  { path: 'notifications', component: NotificationsPageComponent, title: 'Notifications — eCommerce' },
  { path: 'login', component: LoginComponent, title: 'Sign In — eCommerce' },
  { path: 'register', component: RegisterComponent, title: 'Create Account — eCommerce' },
  { path: 'callback', component: CallbackComponent, title: 'eCommerce' },
  { path: '**', redirectTo: 'catalog' },
];
