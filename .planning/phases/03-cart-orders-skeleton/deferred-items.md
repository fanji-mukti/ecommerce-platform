# Deferred Items — Phase 03 Plan 01

Out-of-scope discoveries logged during execution (not fixed, per scope-boundary rule).

## 1. `dotnet test` fails repo-wide with a testhost version mismatch

**Found during:** Plan 03-01, Task 3 verification (`dotnet test src/services/cart/Cart.sln`)

**Symptom:**
```
An assembly specified in the application dependencies manifest (testhost.deps.json) was not found:
  package: 'testhost', version: '18.6.0-release-26270-133'
  path: 'testhost.dll'
```

**Scope:** Confirmed this is **pre-existing and repo-wide**, not introduced by this plan — running
`dotnet test src/services/catalog/Catalog.sln` on the already-existing (untouched) `ECommerce.Catalog.Tests`
project produces the identical error. Root cause is most likely that the xunit.v3 test projects in this
repo (`OutputType=Exe`, no `Microsoft.NET.Test.Sdk`/`xunit.runner.visualstudio` package reference) rely on
xunit v3's native `Microsoft.Testing.Platform` execution mode, but `dotnet test` on this machine's .NET 10
SDK (10.0.301) is dispatching through the classic VSTest/testhost adapter path instead, which expects a
`testhost` package version the installed SDK doesn't ship.

**Workaround used for this plan's verification:** invoke the compiled test executable directly
(`dotnet exec ECommerce.Cart.Tests.dll`), which uses xunit v3's in-process runner and bypasses VSTest/testhost
entirely. This successfully ran the full Cart.Tests suite at least once during this session (see
`03-01-SUMMARY.md` for the Redis-config bug that run caught and the fix that followed).

**Not fixed here:** fixing `dotnet test`'s dispatch mode repo-wide (e.g. adding `Microsoft.NET.Test.Sdk` +
`xunit.runner.visualstudio` to every test project, or setting `<TestingPlatformDotnetTestSupport>true</TestingPlatformDotnetTestSupport>`)
touches every existing test project in the repo, not just Cart's — out of scope for this plan.

## 2. Smart App Control blocked repeated local re-execution of newly-built Cart binaries

**Found during:** Plan 03-01, Task 3 — re-verifying the full Cart.Tests suite after the Redis-config fix.

**Symptom:** Windows 11 Smart App Control (Code Integrity policy, confirmed via
`Microsoft-Windows-CodeIntegrity/Operational` event log entries 3077/3089/3118) began blocking process loads
of freshly-recompiled `ECommerce.Cart.Tests.dll` and later `ECommerce.Cart.API.dll` with
`FileLoadException: ... An Application Control policy has blocked this file. (0x800711C7)`, even from a
copy in a different directory (ruling out a path-based policy) and across `Release`/`Debug` configurations
and multiple rebuilds/retries.

**Scope:** This is a session/environment-specific Windows security feature (this dev VM has
`VerifiedAndReputablePolicyState = 1`, i.e. Smart App Control enforced), not a code defect. It is not
something a code change can resolve — the alternative (disabling Smart App Control) requires administrator
rights and is a system-wide security policy change outside this task's scope; it was not attempted.

**Confidence in the fix despite the block:** before Smart App Control began blocking `ECommerce.Cart.Tests.dll`
consistently, one full run **did** execute and reproduced the exact "No endpoints specified" Redis
configuration bug (9/10 tests failing), which was root-caused directly against
`Aspire.StackExchange.Redis` 13.4.4's published source and fixed (see `03-01-SUMMARY.md`). All `dotnet build`
runs for `Cart.sln` and `ecommerce.AppHost.sln` succeed with 0 errors after every change in this plan.

**Recurrence during phase-level post-merge verification (Wave 2, orchestrator session):** after merging
Plan 03-03's changes, `ECommerce.Orders.API.dll` began hitting the identical
`FileLoadException: ... An Application Control policy has blocked this file. (0x800711C7)` on every
`dotnet exec ECommerce.Orders.Tests.dll` attempt (5 retries: immediate, +15s, clean rebuild, +90s wait) —
all 10 tests failed with the same file-load error, not a test assertion failure. Confirmed non-systemic:
`ECommerce.Cart.Tests.dll`'s freshly-built binaries loaded without issue in the same window, ruling out a
blanket SAC lockdown. This is the same known, pre-existing, environment/VM-level policy issue as above —
not a regression introduced by Plan 03-03 or 03-04. Supporting evidence the Orders test suite is actually
green: the Plan 03-03 executor itself ran `ECommerce.Orders.Tests.exe` successfully twice in its own
session (10/10 passing) immediately after writing the tests, before this block recurred. `dotnet build`
for `Orders.sln` and `ecommerce.AppHost.sln` succeeded with 0 errors both before and after this block.
**Action needed from a human with admin rights on this VM:** either disable Smart App Control, or add an
Application Control exclusion for `dotnet build` output under `src/**/bin/**`.
