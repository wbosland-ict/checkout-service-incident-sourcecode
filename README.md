# checkout-service (sample source) — v2.14.0, post-PR #4821

This is a small, runnable **ASP.NET Core / EF Core, C#** sample that
reproduces the bug from `problem-evidence/pr-4821-diff.md`. It exists so
**Exercise 4 (Fixing the Source Code with AI)** has real source code to
work against, matching the incident scenario used in Exercises 1–3.

It is **not** a full checkout-service — inventory, payment, and order
writing are simplified in-process stubs.

This snapshot matches **v2.14.0**, the version running at the time of the
incident. The code is deliberately left undocumented about what, if
anything, is wrong with it — that's for you and your AI assistant to
work out from the RFC you produced in Exercise 3, your own investigation
of the code, and the evidence files, not from comments or notes in this
repo.

## Project layout

```
src/ShopFast.Checkout/         Class library: cart/product domain, EF Core
                                DbContext, CheckoutService, and stub
                                inventory/payment/order clients.
tests/ShopFast.Checkout.Tests/ xUnit tests.
  Fixtures/                      Sqlite in-memory DbContext factory, a
                                  query-counting interceptor, and a cart
                                  builder for realistic item counts.
  CheckoutServiceTests.cs        Smoke test: checkout completes end-to-end.
```

## Prerequisites

- .NET SDK 9.0+ (`dotnet --version`)

## Build & test

```powershell
dotnet build
dotnet test
```

The suite should build clean and show **1 passed**.

## Working through Exercise 4

Use your own RFC from Exercise 3 (sections 2 and 3) to scope what to
change here, and `checkout-service-incident-files/problem-evidence/` for
supporting evidence. See `exercise-4-fix-with-ai.md` for the task list,
including writing your own regression test under
`tests/ShopFast.Checkout.Tests/`.
"# checkout-service-incident-sourcecode" 
