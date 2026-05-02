# 1Breadcrumb Library — Technical Assessment

Crumb-to-Crumb book lending library: a small full-stack app where Crumbs can register, list books they own, and borrow / return books from each other.

- **Backend:** ASP.NET Core 10 Web API (C#), EF Core 10 + PostgreSQL, JWT bearer auth
- **Frontend:** React 19 + TypeScript + Vite, React Router, Axios
- **Infra:** Docker + docker-compose

---

## Repository layout

```
.
├── src/
│   ├── backend/                       # .NET solution (LibraryApi.slnx)
│   │   ├── LibraryApi.Domain/         # Entities, repository contracts (no deps)
│   │   ├── LibraryApi.Application/    # Services, DTOs, JWT issuer, password hasher
│   │   ├── LibraryApi.Infrastructure/ # EF Core DbContext + repository implementations
│   │   ├── LibraryApi.Web/            # Controllers, Program.cs, Dockerfile
│   │   └── LibraryApi.Tests/          # xUnit + Moq + FluentAssertions
│   └── frontend/
│       ├── src/
│       │   ├── api/      # Axios client, typed endpoint wrappers
│       │   ├── auth/     # AuthContext, ProtectedRoute, LoginPage
│       │   ├── books/    # LibraryPage, BookTable, BookFormModal
│       │   ├── components/ # Modal, Pagination
│       │   └── hooks/    # useDebouncedValue
│       ├── nginx.conf
│       └── Dockerfile
├── docker-compose.yml
├── .env.example
└── docs/superpowers/plans/             # Implementation plan
```

---

## Quick start with Docker (recommended)

Prerequisites: Docker Desktop (or Docker Engine + Compose v2).

```bash
cp .env.example .env       # optional — defaults work for local development
docker compose up --build
```

Then open:

| Service  | URL                      |
| -------- | ------------------------ |
| Frontend | http://localhost:8080    |
| Backend  | http://localhost:5000    |
| Health   | http://localhost:5000/health |

The backend container runs EF Core migrations against PostgreSQL on startup (with retry, so it's safe even if the DB is still warming up).

To stop and clean up:

```bash
docker compose down              # stop containers
docker compose down -v           # also remove the postgres volume
```

---

## Local development (without Docker)

### 1. Postgres

The simplest path is to start just the database from docker-compose:

```bash
docker compose up -d postgres
```

…or use any Postgres 14+ instance. The default connection string is in `src/backend/LibraryApi.Web/appsettings.json`:

```
Host=localhost;Port=5432;Database=librarydb;Username=library;Password=library
```

### 2. Backend

```bash
cd src/backend
dotnet restore
dotnet run --project LibraryApi.Web
```

The API listens on `http://localhost:5000` (see `LibraryApi.Web/Properties/launchSettings.json`). OpenAPI is exposed at `/openapi/v1.json` in Development.

Run the tests:

```bash
dotnet test
```

### 3. Frontend

```bash
cd src/frontend
npm install
npm run dev
```

Vite dev server runs on `http://localhost:5173` and proxies `/api/*` to the backend at `http://localhost:5000` (overridable via `VITE_API_PROXY`).

---

## Using the app

On first boot the API seeds two accounts (idempotent — run again, no duplicates):

| Email             | Password       | Role  | Notes                                              |
| ----------------- | -------------- | ----- | -------------------------------------------------- |
| `admin@bread.com` | `AdminPass123` | Admin | Sees the global activity log at `/admin/activity`. |
| `test@bread.com`  | `TestPass123`  | User  | Owns three demo books out of the box.              |

You can also register fresh accounts at `/login` — toggle to "Register".

1. Sign in. Regular users land on `/library`; admins land there too and see an extra **Activity log** link in the header.
2. Add a book via the floating **+ Add Book** button. The authenticated user becomes the owner.
3. Other users can search, filter (Available / Unavailable), and borrow your books.
4. The borrower (or the owner) can return the book; only the owner can edit or delete.
5. Admins can review every create / update / delete / borrow / return event at `/admin/activity`.

---

## API summary

The canonical contract lives at `src/backend/LibraryApi.Web/OpenApi/library-api.yaml` and is served by the running API at:

```
GET /openapi/v1.yaml
```

The path conventions (`/books` for the collection, `/book` and `/book/{id}` for individual operations) and the `Book` / `BookWithId` schema split mirror the reference spec at https://libapi.1breadcrumb.com/. Extensions over that spec — auth, owner / borrower fields, DELETE, borrow / return, search & pagination — are documented inline.

All `/api/book*` endpoints require `Authorization: Bearer <token>`.

| Method | Path                          | Description                        |
| ------ | ----------------------------- | ---------------------------------- |
| POST   | `/api/auth/register`          | Create an account, returns JWT     |
| POST   | `/api/auth/login`             | Log in, returns JWT                |
| GET    | `/api/books`                  | Paged list. Query: `search`, `availability` (`All`/`Available`/`Unavailable`), `page`, `pageSize` |
| POST   | `/api/book`                   | Create — caller becomes the owner  |
| GET    | `/api/book/{id}`              | Single book                        |
| PUT    | `/api/book/{id}`              | Update title (owner only)          |
| DELETE | `/api/book/{id}`              | Delete (owner only)                |
| POST   | `/api/book/{id}/borrow`       | Borrow if available (not owner)    |
| POST   | `/api/book/{id}/return`       | Return (borrower or owner)         |
| GET    | `/api/admin/activity`         | Paginated audit log (Admin role)   |
| GET    | `/openapi/v1.yaml`            | OpenAPI 3.0 contract               |
| GET    | `/health`                     | Liveness probe                     |

---

## Design notes

**Architecture (N-layer with abstraction).** Dependencies flow inward:

```
Web ──► Application ──► Domain
   ╲          ▲
    ╲         │
     ──► Infrastructure ──► Domain
```

- `LibraryApi.Domain` holds entities and the repository / service contracts. It depends on nothing.
- `LibraryApi.Application` contains the use cases (`BookService`, `AuthService`), DTOs, the password hasher and JWT generator. It depends only on Domain.
- `LibraryApi.Infrastructure` provides EF Core implementations of the repository interfaces.
- `LibraryApi.Web` composes the system: controllers, JWT auth setup, CORS, exception middleware, dependency injection.

Each layer exposes an `AddX(...)` extension method (`AddApplication`, `AddInfrastructure`) so the composition root in `Program.cs` stays small.

**SOLID applied:**

- *Single responsibility* — repositories only do persistence; services only do orchestration / business rules; controllers only translate HTTP. The password hasher and JWT generator are separate small types rather than living inside `AuthService`.
- *Open/closed* — services depend on `IBookRepository` / `IUserRepository` / `IPasswordHasher` / `IJwtTokenGenerator`. Swapping implementations (e.g. for tests) requires no changes to consumers.
- *Liskov / interface segregation* — interfaces are small and intent-revealing; the test suite uses Moq fakes interchangeably.
- *Dependency inversion* — high-level modules (Application) depend on abstractions, not on EF Core or BCrypt.

**Authentication.** JWT bearer (HS256). Registration hashes the password with BCrypt; login verifies and returns `{ token, expiresAt, user }`. The frontend stores the token in `localStorage`, attaches it via an Axios interceptor, and a 401 response triggers a global logout.

**Authorization rules in `BookService`:**

- Update / Delete: owner only.
- Borrow: book must be available; owner cannot borrow their own book.
- Return: borrower or owner.

These rules live in the service so they're enforced regardless of which controller (or future delivery mechanism) invokes them.

**Error handling.** Domain exceptions (`NotFoundException`, `ForbiddenException`, `ConflictException`, base `DomainException`) are translated to 404 / 403 / 409 / 400 JSON responses by `ExceptionHandlingMiddleware`. Anything else becomes a 500 with a logged stack trace.

**Persistence.** PostgreSQL via Npgsql + EF Core 10. The schema is defined in `LibraryDbContext`:

- `users`: unique index on `email`. `Role` is a string column (`User` / `Admin`).
- `books`: indexes on `title` (for search) and `borrower_id`. The owner FK uses `Restrict` (you can't delete a user who owns books); the borrower FK uses `SetNull` (returns the book if the borrower account is removed).
- `book_activities`: append-only audit log indexed by `OccurredAt`, `BookId`, and `ActorId`. `BookId` and `ActorId` are intentionally **not** foreign keys — book deletions and user renames must not corrupt the historical record. Title and actor name are snapshotted at write time.

Migrations are applied at startup with retry — the backend container waits for Postgres to become reachable. After migrations the `DataSeeder` idempotently inserts the demo accounts and a few sample books.

**Activity logging.** `BookService` records a `BookActivity` row for every mutation (create, update, delete, borrow, return). Admins read the log via `GET /api/admin/activity`. The `AdminController` is gated with `[Authorize(Roles = "Admin")]`; the role flows through the JWT `role` claim emitted by `JwtTokenGenerator`.

**Search.** Title search uses `ILIKE` for case-insensitive matching, sorted by title for stable pagination.

**Frontend structure.** Feature folders (`auth/`, `books/`) over technical folders. The Library page composes three small components — `BookTable`, `Pagination`, `BookFormModal` — instead of being a 500-line monolith. A debounced search hook keeps the URL/server in sync without spamming the API.

---

## Testing

### Backend (xUnit)

```bash
cd src/backend
dotnet test LibraryApi.Tests
```

Covers `BookService` (borrow / return state machine, ownership rules, activity logging), `AuthService` (registration / login flows), and `AdminService` (activity-log mapping).

### Frontend UI tests (Playwright, mocked network)

Fast, isolated, run against `npm run dev`. The backend is mocked via `page.route()` — no docker, no Postgres needed.

```bash
cd src/frontend
npm install
npx playwright install --with-deps chromium    # one-time
npm run test:ui
```

Covers the login page (form behavior, validation, success/error flows), the library page (rendering, search, borrow, owner-only edit/delete, admin-link visibility, add-book modal), and the admin page (table render, role gating, redirect behavior).

### Frontend E2E tests (Playwright, full stack)

Run against the actual docker-compose stack — real backend, real Postgres, seeded users.

```bash
docker compose up -d --build
cd src/frontend
npm run test:e2e
docker compose down -v
```

Covers seeded login, registration → first book flow, full book lifecycle (add / edit / delete), borrow + return between users, admin activity log assertions, and role gating on `/admin/activity`.

### Continuous integration

`.github/workflows/ci.yml` runs on every PR against `main` and on every push to `main`:

| Job        | What it does                                                                  |
| ---------- | ----------------------------------------------------------------------------- |
| `backend`  | `dotnet restore`, `build -c Release`, `dotnet test` against the unit suite.   |
| `frontend` | `npm install`, `npm run build`, `npm run test:ui` (Playwright UI tests).      |
| `e2e`      | Builds the docker stack, waits for `/health`, runs `npm run test:e2e`. Depends on `backend` and `frontend` passing. |

Playwright HTML reports and compose logs are uploaded as artifacts on every run.

The `main` branch is protected to require the `e2e` job to pass before merge — you cannot push to `main` or merge a PR that breaks the end-to-end suite.

---

## Configuration reference

| Variable                  | Where it's used                        | Default                                       |
| ------------------------- | -------------------------------------- | --------------------------------------------- |
| `ConnectionStrings__Default` | Backend                              | `Host=localhost;...` (appsettings)            |
| `Jwt__SigningKey`         | Backend                                | Dev key in `appsettings.Development.json`     |
| `Jwt__ExpiryMinutes`      | Backend                                | `480` (8 hours)                               |
| `Cors__AllowedOrigins__N` | Backend                                | `http://localhost:5173`, `http://localhost:3000` |
| `VITE_API_BASE_URL`       | Frontend (build-time)                  | `/api` (uses Vite proxy / nginx)              |
| `VITE_API_PROXY`          | Vite dev server                        | `http://localhost:5000`                       |

---

## What I'd do next

- Integration tests against a real Postgres via Testcontainers.
- Frontend component / E2E tests (Vitest + Playwright).
- A loan history table instead of a single nullable `BorrowerId`, so you can see who *has* borrowed a book over time.
- Refresh tokens + httpOnly cookie storage to harden auth.
- Optimistic UI updates on borrow / return.
