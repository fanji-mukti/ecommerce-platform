---
status: diagnosed
phase: 02-identity-catalog-gateway
source: [02-VERIFICATION.md]
started: 2026-06-17T00:00:00Z
updated: 2026-06-19T00:00:00Z
---

## Current Test

App shell rendering — blocked by AppHost crash

## Tests

### 1. App shell renders
expected: App shell renders with mat-toolbar, 'eCommerce' logo, 'Catalog' nav link, and 'Sign In' button; router-outlet is visible
result: failed — AppHost crashes before any service starts

### 2. Catalog browse without login
expected: Product grid loads with 'Browse Products' h1, category filter chips (Electronics, Clothing, Books, Home, Sports), and paginated product cards
result: [pending]

### 3. PKCE login flow (demo@example.com / demo123)
expected: Browser redirects to http://localhost:5005/Account/Login, then back to /callback, then to /catalog with 'Sign Out' button and username visible in toolbar
result: [pending]

### 4. User registration (/register)
expected: POST /api/identity/register returns 201, Angular navigates to /login
result: [pending]

### 5. Product detail view
expected: Navigates to /product/{id} showing product name, price, category, stock badge, disabled 'Add to Cart — Coming Soon' button, and 'Back to Catalog' link
result: [pending]

### 6. OIDC discovery endpoint (http://localhost:5005/.well-known/openid-configuration)
expected: JSON response with issuer, authorization_endpoint, token_endpoint, userinfo_endpoint fields
result: [pending]

## Summary

total: 6
passed: 0
issues: 1
pending: 5
skipped: 0
blocked: 5

## Gaps

### Gap 1: AppHost crashes on startup — cannot run any UAT
status: failed
debug_session:
  error: "System.InvalidOperationException: The endpoint 'http' for resource 'catalog' requested a proxy (IsProxied is true). Non-container resources cannot be proxied when both TargetPort and Port are specified with the same value."
  root_cause: "AppHost/Program.cs uses WithEndpoint(port: X, targetPort: X) on all project resources. In Aspire 10, setting port == targetPort on a non-container proxied resource is invalid — Aspire DCP can't bind both to the same port. Fix: replace all WithEndpoint patterns with WithHttpEndpoint(port: X) and remove the duplicate http endpoint on Payments."
  files_to_fix:
    - src/ecommerce.AppHost/Program.cs
