# Developer guide

Everything you need to get this running and to change it without breaking
something. Read [ARCHITECTURE-NOTES.md](ARCHITECTURE-NOTES.md) as well — it
covers what is deliberately unfinished, and there are traps in there.

---

## What this is

A scheduling and service-history application for an independent auto repair
shop. Customers request appointments from a public page; staff confirm them,
run the day off a schedule board, and keep a service history against each
vehicle.

ASP.NET Core 8 MVC, EF Core, SQL Server, Bootstrap 5. Server-rendered Razor
throughout — no SPA, no client-side API surface.

---

## Getting it running

### You need

- .NET 8 SDK
- SQL Server (LocalDB or a full instance) reachable at `Server=.`
- Visual Studio 2022+ or `dotnet` CLI

### ⚠️ The database scripts are gitignored

`Gregs Auto.DAL/Scripts/` is **not in the repository**. A fresh clone builds and
passes the unit tests, and cannot create a database or run the smoke tests.

If you don't have those files, get them from whoever gave you the repo before
going further. Nothing below works without them.

### Build the database

Run these against your SQL Server, **in this order**. Each depends on the ones
before it.

| # | Script | What it does |
|---|---|---|
| 1 | `CreateDatabase.sql` | Creates `GregsAuto` and the core tables |
| 2 | `SeedData.sql` | Sample customers, vehicles, services, appointments |
| 3 | `SetStaffPasswords.sql` | Real password hashes on the three staff accounts |
| 4 | `AddUserControls.sql` | Lockout state, `LoginAudit` |
| 5 | `AddBookingRequests.sql` | The public request queue |
| 6 | `AddArchiving.sql` | `IsActive` on services, customers, vehicles |
| 7 | `AddShop.sql` | The `Shops` table and the shop this deployment serves |
| 8 | `SeedShops.sql` | Two more shops (data only — see the notes) |
| 9 | `AddShopScoping.sql` | `ShopId` on every tenant-scoped table |
| 10 | `FixShopIdDefaults.sql` | **Required.** Without it every INSERT fails |
| 11 | `AddAppointmentSnapshot.sql` | Price and duration copied onto each appointment |

Two optional extras:

- `RefreshDemoData.sql` — re-anchors the sample appointments to today. Run it
  before a demo so the schedule doesn't look stale.
- `SeedBookingRequests.sql` — five pending and two handled requests, so
  `/Appointments/Requests` has something in it. Every pending one is valid
  against the booking rules, so they can all actually be accepted.

### Run it

Connection string lives in `Gregs Auto/appsettings.json` under
`ConnectionStrings:GregsAutoContext`. The default expects a local instance.

```
dotnet run --project "Gregs Auto"
```

### Sign in

Three seeded accounts, all with password `GregsAuto123!`:

| Email | Role | Sees |
|---|---|---|
| `greg@gregsauto.com` | Admin | Everything, including Staff |
| `lisa@gregsauto.com` | Manager | Everything except Staff |
| `omar@gregsauto.com` | Technician | Schedule and read-only customers |

Five wrong passwords locks an account for 15 minutes. Clear it from the Staff
page, or `UPDATE Users SET LockedOutUntil = NULL, FailedLoginCount = 0`.

---

## How the code is laid out

Five projects. Dependencies point one way only — the Domain knows nothing about
the web or the database.

```
Gregs Auto.Domain      business rules, entities, repository interfaces
  ├── Scheduling/      appointments, booking requests, booking rules
  ├── Catalog/         the service list
  ├── Identity/        users, roles, passwords, audit
  ├── Shared/          clock, shop settings, shop record, unit of work
  ├── Licensing/       tiers and feature flags
  ├── EntityModels/    shared across modules
  └── IRepositories/   shared across modules

Gregs Auto.DAL         EF Core context, repository implementations, SQL scripts
Gregs Auto             MVC controllers, views, view models, DI wiring
Gregs Auto.Tests       unit tests, in-memory fakes, no database
Gregs Auto.SmokeTests  the real app against a real database
```

**Modules import each other explicitly.** The web and test projects take global
usings for all of them because they orchestrate everything, but inside the
Domain a dependency between modules shows up as a `using` in the diff. That's
the enforcement mechanism — keep it that way.

---

## How a request flows

```
Controller  →  Logic  →  Repository  →  DbContext  →  SQL Server
```

- **Controller** — maps a form to a view model, calls one logic method, maps the
  result to a view. No business rules.
- **Logic** — every rule lives here. Depends only on repository interfaces, so
  it's testable without a database.
- **Repository** — data access and EF `Include`s. No rules.

The important consequence: **anything that decides whether an action is allowed
belongs in the Logic layer.** Not the controller, and never only in the view.

---

## The processes

### 1. A customer requests an appointment

This is the main flow and the one most worth understanding.

```
public form  →  BookingRequests row  →  staff queue  →  accept  →  real records
```

1. A visitor fills in `/Appointments/Schedule` — their name, phone, vehicle
   (year/make/model as free text), the service, and a preferred time. **No
   account, no password.**
2. `BookingRequestLogic.SubmitAsync` checks the time is in the future, inside
   opening hours, long enough for the job to finish before closing, and that the
   service exists and is active.
3. It writes a row to `BookingRequests` and **stops**. No customer is created,
   no vehicle, no appointment.
4. Staff see it at `/Appointments/Requests` and either accept or decline.
5. `AcceptAsync` creates the customer (or matches an existing one **by phone
   number**), creates the vehicle (or matches on owner + year/make/model), then
   books the appointment through the ordinary rules.
6. The request records who handled it, when, and which appointment it became.

**Why the middle step exists:** anyone on the internet can post that form.
Nothing they type is allowed into the customer records until a staff member has
looked at it. That is also what lets the public page exist at all without
exposing the customer list.

**Accepting is one transaction.** If the booking is refused at step 5 — no free
bay, outside hours — the customer and vehicle created moments earlier are rolled
back and the request stays Pending, so staff can offer another time.

### 2. Staff book directly

Signed-in staff get a different form on the same page: a dropdown of vehicles
already on file, straight onto the schedule. Same rules, no queue.

### 3. Booking rules

Enforced in `Scheduling/AppointmentLogic`, on both paths above:

| Rule | Detail |
|---|---|
| In the future | Compared against **wall-clock time at the shop**, not the server |
| Inside opening hours | Between `OpensAt` and `ClosesAt` |
| Not a closed day | Configured per shop |
| The whole job fits | 90-minute job at 4:30pm is refused when you close at 5 |
| Service active | Archived services keep their id — checked in logic, not just the dropdown |
| Vehicle active | Same reasoning |
| No vehicle overlap | Interval overlap using the service duration, not equality |
| Bay capacity | Refuses when overlapping jobs already equal `BayCount` |

Back-to-back is fine — intervals are half-open, so a job starting exactly when
another ends is allowed.

### 4. A job through the day

```
Scheduled  →  InProgress  →  Completed
     └──────────────────→  Cancelled
```

`Completed` and `Cancelled` are terminal. A cancelled slot frees up again; a
completed one stays on the vehicle as history.

### 5. Signing in

`Identity/UserLogic.AuthenticateAsync`:

- Unknown email and wrong password return **the same result**, so the form can't
  be used to find out which addresses exist.
- Every attempt is written to `LoginAudit` with outcome and IP. Attempts against
  unknown addresses are kept with a null `UserId` — those are what probing looks
  like.
- Five failures locks the account for 15 minutes; the correct password is
  refused while locked.
- `IsActive` is only checked **after** the password verifies, so guessing can't
  reveal that an account exists but is deactivated.

### 6. Archiving, never deleting

Services, customers and vehicles are archived via `IsActive`. Each carries
history a delete would destroy — that history is half the point of the product.
Archived records disappear from working lists and stay attached to past jobs.

Staff accounts work the same way.

### 7. Shop settings and tiers

`Shops` holds how the shop runs — bays, hours, closed days, timezone — plus
`Tier`, which is what they've paid for.

Settings are editable at `/Settings`. **Tier is not**, and it's protected three
ways so there's no single check to forget: the view model has no tier property,
the update carrier has no tier field, and the logic never assigns one.

Settings are cached (`IShopContext`) and `Reload()` is called after a save, so
booking rules pick up new hours without a restart.

Tiers are an ordered chain — `Scheduling` ⊂ `Invoicing` ⊂ `Inspections` — so
three tiers means three configurations rather than eight. Nothing uses the two
paid tiers yet; they're the socket future work plugs into.

---

## Conventions

These are the ones a newcomer usually breaks first.

**Business rules go in the Logic layer.** If you find yourself writing an `if`
in a controller that decides whether something is allowed, it's in the wrong
place.

**Failures are results, not exceptions.** "All three bays are taken" is an
outcome. Every logic method returns a result type implementing `IOperationResult`.
This matters beyond style — `IUnitOfWork` rolls back on `Success == false`, so
throwing instead would change transactional behaviour.

**Check state in logic, not only in the view.** An archived service still has a
valid id, and a stale form will happily post one. Leaving it out of a dropdown
is presentation; refusing it in `BookAsync` is the control.

**Never bind an entity to a form.** View models only. That's what stops
overposting, and it's what stops a shop granting itself a tier.

**Anti-forgery on every POST.** No exceptions — currently 100% coverage.

**Never call `DateTime.Now`.** Use `IShopClock`. `LocalNow` is wall-clock time
at the shop; `UtcNow` is for audit stamps. A UTC-hosted server would otherwise
reject same-day bookings as being in the past.

**Never `new` a DbContext.** It's injected, one per request. Doing otherwise
gives each repository its own connection and silently breaks transactions —
this used to be the case and it made `IUnitOfWork` a no-op.

**Wrap multi-entity operations in `IUnitOfWork`.** Anything that writes to more
than one table in one business action.

**Archive, don't delete.**

---

## Adding a feature — worked example

Say you're adding vehicle mileage tracking.

1. **Schema** — new script in `Gregs Auto.DAL/Scripts/`, additive and
   idempotent, guarded with `IF COL_LENGTH(...) IS NULL`. Add it to the ordered
   list in this document **and** to `TestDatabase.Scripts` in the smoke tests, or
   they'll run against a schema that doesn't have it.
2. **Entity** — a property on a `*.Partial.cs` file, not the scaffolded entity,
   so regenerating doesn't wipe it.
3. **Context** — configure it in `GregsAutoContext.Model.cs`, not the generated
   `GregsAutoContext.cs`.
4. **Logic** — rules and validation in the right Domain module, returning a
   result type.
5. **Controller + view model + view** — mapping only.
6. **DI** — register in `Program.cs`.
7. **Tests** — unit tests for the rules; a smoke test if it touches a new
   write path.

---

## Tests

```
dotnet test "Gregs Auto.Tests"        89 tests, no database, run constantly
dotnet test "Gregs Auto.SmokeTests"   12 tests, real database, run after schema changes
```

**Unit tests** use in-memory fakes and a `TestClock` pinned to a fixed moment —
a July morning specifically, so shop-local and UTC differ by five hours and a
test comparing against the wrong one fails rather than passing by luck.

**Smoke tests** build a throwaway `GregsAuto_SmokeTests` database from the
migration scripts and drive the real application through the real HTTP pipeline.
They exist because the unit suite is structurally blind to broken migrations,
mis-registered services, and DI mistakes — all three of which have happened.

Run the smoke tests after **any** schema change. They need SQL Server and the
gitignored scripts.

---

## Gotchas

**Rate limiting will bite you while testing.** Five booking submissions per ten
minutes per IP, twenty sign-in attempts per five. You'll get a bare `429` with
no friendly message and conclude something is broken. It isn't.

**`datetime-local` inputs need `asp-format`.** A `DateTime` renders as
`07/29/2026 09:00:00` by default, which the input silently rejects — the field
just appears blank with no error anywhere. Use
`asp-format="{0:yyyy-MM-ddTHH:mm}"`.

**The honeypot returns success.** A bot filling the hidden `Website` field gets
the normal thank-you page and writes nothing. If a submission "succeeds" but no
row appears, check whether your test is filling every field on the form.

**Settings are cached.** Editing the `Shops` row directly in SQL won't take
effect until the app restarts or something calls `IShopContext.Reload()`. The
settings screen does; a manual `UPDATE` doesn't.

**Two seeded appointment statuses are terminal.** If a status change appears to
do nothing, check whether the job is already Completed or Cancelled — the logic
no-ops rather than erroring.

**Multi-tenancy is not enforced.** Every table has a `ShopId` and nothing reads
it. Read the warning in [ARCHITECTURE-NOTES.md](ARCHITECTURE-NOTES.md) before
adding a second shop's users.
