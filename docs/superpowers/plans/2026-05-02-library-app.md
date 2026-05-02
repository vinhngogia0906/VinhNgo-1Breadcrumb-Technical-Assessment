# Library Book Management — Implementation Plan

**Goal:** Build an MVP Crumb-to-Crumb book lending library web app with React frontend, .NET 10 backend, PostgreSQL, JWT auth, and Docker.

**Architecture:** N-layer .NET solution (Domain → Application → Infrastructure → Web) with repository + service abstractions. React + Vite + TS SPA served via nginx. Postgres for persistence. JWT bearer authentication for all book endpoints.

**Tech Stack:**
- Backend: .NET 10, ASP.NET Core, EF Core 10 (Npgsql), JWT Bearer
- Frontend: React 19 + TypeScript + Vite, React Router, Axios
- DB: PostgreSQL 16
- Infra: Docker, docker-compose

---

## File Structure

```
/
├── src/
│   ├── backend/
│   │   ├── LibraryApi.sln
│   │   ├── LibraryApi.Domain/         # Entities, repository/service interfaces
│   │   ├── LibraryApi.Application/    # Service implementations, DTOs
│   │   ├── LibraryApi.Infrastructure/ # EF Core DbContext + repository implementations
│   │   ├── LibraryApi.Web/            # Controllers, Program.cs, Dockerfile
│   │   └── LibraryApi.Tests/          # Unit tests
│   └── frontend/
│       ├── src/
│       │   ├── api/        # Axios client + endpoints
│       │   ├── auth/       # Auth context, login page
│       │   ├── books/      # Library page, BookTable, BookFormModal
│       │   ├── components/ # Reusable UI (Modal, Pagination, Spinner)
│       │   └── App.tsx
│       ├── Dockerfile
│       └── nginx.conf
├── docker-compose.yml
├── README.md
```

---

## Domain Model

**User**: Id (Guid), Email (unique), DisplayName, PasswordHash, CreatedAt
**Book**: Id (Guid), Title, OwnerId (FK→User), BorrowerId (nullable FK→User), CreatedAt, UpdatedAt
- Availability is derived: `BorrowerId == null` ⇒ available

**Endpoints:**
- `POST /api/auth/register` → `{token, user}`
- `POST /api/auth/login` → `{token, user}`
- `GET /api/books?search=&availability=&page=&pageSize=` → paginated list (auth required)
- `POST /api/books` → create with current user as Owner
- `PUT /api/books/{id}` → update title (owner-only)
- `DELETE /api/books/{id}` → delete (owner-only)
- `POST /api/books/{id}/borrow` → set BorrowerId = current user
- `POST /api/books/{id}/return` → clear BorrowerId

---

## Tasks

### Task 1: Backend solution skeleton
- Create solution + 5 projects
- Wire project references: Web→App+Infra, App→Domain, Infra→Domain+App
- Verify builds

### Task 2: Domain layer
- `Book`, `User` entities
- `IBookRepository`, `IUserRepository` interfaces
- `IBookService`, `IAuthService` interfaces
- DTOs in Application: `BookDto`, `CreateBookDto`, `UpdateBookDto`, `LoginDto`, `RegisterDto`, `AuthResponseDto`, `PagedResult<T>`

### Task 3: Infrastructure layer
- `LibraryDbContext` with DbSets and Postgres config
- `BookRepository`, `UserRepository` implementing interfaces
- DI registration extension method

### Task 4: Application layer
- `AuthService` (BCrypt password hash, JWT issuance)
- `BookService` (orchestrates repository, enforces ownership rules)
- DI registration extension method

### Task 5: Web/API layer
- `AuthController`, `BooksController`
- `Program.cs` — JWT bearer, CORS, EF Core, controllers, OpenAPI
- `appsettings.json` (JWT secret, conn string)
- Apply migrations on startup (dev convenience)

### Task 6: Backend tests
- BookService unit tests (in-memory repo or moq) — borrow/return/ownership
- AuthService unit test — register/login happy + wrong password

### Task 7: Frontend skeleton
- Vite React-TS scaffold
- Axios client w/ JWT interceptor
- AuthContext + ProtectedRoute
- React Router routes: `/login`, `/library`

### Task 8: Login page
- Email + password form, register toggle
- Calls `/auth/login` or `/auth/register`, stores token, redirects

### Task 9: Library page
- BookTable: columns Book, Owner, Availability, Actions (borrow/return, edit, delete)
- Search input (Book title), availability filter (all/available/unavailable), pagination
- AddBook FAB → opens BookFormModal
- Edit → opens BookFormModal in edit mode
- Borrow toggle: calls borrow if available, return if borrowed by me; disabled if borrowed by others (or owner-only)
- Delete: confirm + delete (owner-only)

### Task 10: Dockerfiles + compose
- Backend Dockerfile: multi-stage build (sdk → runtime)
- Frontend Dockerfile: Node build → nginx serve, with nginx.conf proxying `/api` to backend
- docker-compose.yml: postgres + backend + frontend with depends_on + healthchecks

### Task 11: README
- Prerequisites, run with docker-compose, dev workflow, project structure, design notes

---

## Notes
- Use BCrypt.Net-Next for password hashing
- JWT: HS256, claims = user id (sub) + email
- EF Core auto-migrate on startup for dev simplicity (note in README)
- Frontend dev: Vite proxy `/api` → `http://localhost:5000`
- Production: nginx in frontend container proxies `/api` to backend service
