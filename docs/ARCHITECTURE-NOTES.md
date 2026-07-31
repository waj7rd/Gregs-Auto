# Architecture notes

Short notes on things that are true about this codebase but not obvious from
reading it. Kept deliberately brief — this is a warning sign, not a manual.

---

## ⚠️ Multi-tenancy is NOT enforced

**The schema looks multi-tenant. The application is not.**

Seven tables carry a `ShopId` column with foreign keys, indexes, and composite
constraints that prevent a child row drifting to a different shop than its
parent:

```
Customers, Vehicles, Services, Appointments, Users, BookingRequests   ShopId NOT NULL
LoginAudit                                                            ShopId NULL
```

There are three shops in the `Shops` table.

**Nothing reads any of it.** There are no EF global query filters and no tenant
resolution. `ShopContextProvider` serves whichever shop has the lowest `ShopId`,
and every query in the application returns rows from every shop.

### What that means in practice

If you add a staff account for shop 2 and sign in as them, they will see shop
1's customers, vehicles, appointments and service catalog. Nothing will error.
Nothing will look wrong. It will simply be a data breach between two of your
customers.

**Do not add users for a second shop until the work below is done.**

### Finishing it, in this order

The order matters. Doing resolution before filtering produces an application
that confidently serves the wrong data.

1. **EF global query filters** on every tenant-scoped entity, in
   `GregsAutoContext.Model.cs`. This is what makes scoping automatic rather
   than something you remember at ~40 query sites. Remembering at 39 of them
   is the same as remembering at none.
2. **Assign `ShopId` on insert** — currently nothing sets it; the database
   default and the backfill are carrying it.
3. **Tenant resolution** — a claim on the staff cookie for the staff side, and
   something explicit (subdomain or route) for the public booking pages, which
   have no signed-in user to derive a shop from.
4. **Make `ShopContextProvider` per-tenant** — `Current` stops meaning "the
   shop" and starts meaning "the shop this request belongs to". It is a
   singleton with a single cached row today; it becomes a keyed cache.

### Or park it deliberately

Also a fine answer. The columns are harmless sitting unused, and the constraints
are correct. Just don't mistake their presence for enforcement.

---

## Things that are enforced, and where

Business rules live in the Domain and are checked on every path — the public
form and staff booking alike. None of them are UI-only, because an archived
record still has a valid id and a stale form will happily post one.

| Rule | Enforced in |
|---|---|
| Booking: future, opening hours, whole job fits, overlap, bay capacity | `Scheduling/AppointmentLogic` |
| Archived service or vehicle can't be booked | `Scheduling/AppointmentLogic` |
| Anonymous input never reaches customer records | `Scheduling/BookingRequestLogic` |
| Lockout, enumeration resistance, last-Admin guards | `Identity/UserLogic` |
| A shop cannot change its own tier | `Shared/ShopLogic` + the view model having no tier property |

---

## ⚠️ `ShopId` defaults are a stopgap

`FixShopIdDefaults.sql` puts a `DEFAULT (1)` on every non-nullable `ShopId`,
because the columns were added `NOT NULL` and nothing in the code sets them —
which broke every insert until the default was added.

**Remove those defaults when tenant resolution lands.** At that point `ShopId`
has to be set explicitly from the request's tenant. A silent default would be
worse than a failure: it would quietly file one shop's data under another.

## Known gaps

**One DbContext per request, and it matters.** `GenericRepository` used to do
`new C()`, giving every repository its own context on its own connection. That
made `IUnitOfWork` silently useless — it opened a transaction on a context
nobody else wrote through, so nothing rolled back. The context is injected now.
Do not reintroduce `new` there.

**Prices are not snapshotted.** `Appointment` reads its price from `Service`, so
changing a price silently restates what past jobs appear to have cost. Harmless
for a schedule, disqualifying for invoicing. Fix before building invoicing, not
after.

**No per-record ownership checks.** Every action takes a bare `id`. Correct
single-tenant; see the warning at the top.

**`Scripts/` is gitignored.** The SQL that builds this schema is local only. A
fresh clone cannot create the database. Back those files up somewhere.
