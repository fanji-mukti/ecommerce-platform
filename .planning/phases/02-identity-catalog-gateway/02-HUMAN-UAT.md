---
status: partial
phase: 02-identity-catalog-gateway
source: [02-VERIFICATION.md]
started: 2026-06-20T08:00:00Z
updated: 2026-06-20T08:00:00Z
---

## Current Test

[awaiting human testing]

## Tests

### 1. App shell renders correctly
expected: App shell renders with mat-toolbar, 'eCommerce' logo, 'Catalog' nav link, and 'Sign In' button; router-outlet is visible
result: [pending]

### 2. Catalog page loads without login
expected: Product grid loads with 'Browse Products' h1, category filter chips, and paginated product cards
result: [pending]

### 3. PKCE login flow with demo@example.com / Demo123!
expected: Browser redirects to Identity login, then back to /callback, then to /catalog with 'Sign Out' button and username visible
result: [pending]

### 4. Register new account (new-user@example.com / password123)
expected: POST /api/identity/register returns 201, Angular navigates to /login
result: [pending]

### 5. Product detail page
expected: /product/{id} shows product name, price, category, stock badge, disabled 'Add to Cart — Coming Soon' button, and 'Back to Catalog' link
result: [pending]

### 6. OIDC discovery endpoint
expected: GET http://localhost:5005/.well-known/openid-configuration returns JSON with issuer, authorization_endpoint, token_endpoint, userinfo_endpoint
result: [pending]

## Summary

total: 6
passed: 0
issues: 0
pending: 6
skipped: 0
blocked: 0

## Gaps
