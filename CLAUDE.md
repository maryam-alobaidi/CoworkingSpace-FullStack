# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Vantage Coworking Space: workspace bookings, event ticket sales (Stripe Checkout), an admin dashboard, and an AI chat assistant (Semantic Kernel + OpenAI `gpt-4o-mini`). Two independent apps:

- `Backend/`: .NET 9 Web API, solution at `Backend/CoworkingSpace.API/CoworkingSpace.API.sln`
- `Frontend/`: Angular 21 (standalone components, signals, Bootstrap 5, ngx-toastr)

Code comments are often in Arabic.

## Commands

Backend (run from `Backend/CoworkingSpace.API`):
```
dotnet build CoworkingSpace.API.sln
dotnet run                      # Scalar API docs at /scalar in Development
```
There are no backend test projects.

Frontend (run from `Frontend/`):
```
npm install --legacy-peer-deps  # same flag the Dockerfile uses
npm start                       # ng serve → http://localhost:4200
npm run build                   # output: dist/Frontend/browser
npm test                        # Vitest via @angular/build:unit-test
npx ng test --include src/app/services/auth.spec.ts   # single spec file
```
Formatting is Prettier (`Frontend/.prettierrc`). There is no lint target.

Full stack in Docker (from the repo root): `docker compose up --build`. This starts SQL Server, runs `db-init/init.sql`, which restores `db-init/CoworkSpaceDB.bak` (database `CoworkSpaceDB`), then starts the backend on :8080 and the frontend on :4200 (nginx). Secrets come from the root `.env`: `JWT_KEY`, `STRIPE_SECRET_KEY`, `STRIPE_WEBHOOK_SECRET`, `EMAIL_APP_PASSWORD`, `OPENAI_API_KEY`. For local `dotnet run`, set them with `dotnet user-secrets` (for example `Stripe:WebhookSecret`, which is the `whsec_...` value printed by `stripe listen --forward-to localhost:8080/api/webhook`).

CI (`.github/workflows/docker-build.yml`) only builds the two Docker images and pushes them to Docker Hub. It runs no tests.

## Backend architecture

There are four projects with a layered dependency chain: **API → BLL → DAL → Models**.

- **Models** (`CoworkingSpace.Models`): plain DTOs (`*Model`, `*DTO`). They are shared by every layer.
- **DAL** (`CoworkingSpace.DAL`): static `cls<Entity>Data` classes that use raw ADO.NET (`Microsoft.Data.SqlClient`). **All data access goes through stored procedures** (`Sp_AddNew<Entity>`, `Sp_Update<Entity>`, `Sp_Get<Entity>ByID`, and so on). The schema and procedures exist only in the database backup `db-init/CoworkSpaceDB.bak`; there is no SQL source in the repo. A new query means creating the procedure in the database, adding a DAL method, and refreshing the `.bak` so Docker picks it up. Roles in the database: `Admin` (Id 4) and `Member` (Id 5). `clsPrimaryFunctions` holds the static connection string (set once in `Program.cs` from `ConnectionStrings:DefaultConnection`) and shared helpers (`AddAsync` reads an OUTPUT param for the new ID, `UpdateAsync`, `DeleteAsync`, `GetAsync` returns a reader with `CloseConnection`).
- **BLL** (`CoworkingSpace.BLL`): `cls<Entity>` classes that use an active-record style. The pattern is:
  - `enMode { addNew, update }` plus `Save()`, which dispatches to `_AddNew...` or `_Update...`.
  - A private constructor from the model sets `Mode = update`, and a static `Find(id)` uses it.
  - Static pass-through methods for list/aggregate queries.

  New entities should follow this pattern. `Services/` holds the DI-registered services (`EmailService`, `AiAgentService`). `Plugins/` holds the Semantic Kernel plugins.
- **API**: controllers mostly call BLL static methods directly. DI is used only for `IEmailService` and `IAiAgentService`.

### Cross-cutting flows

- **Payments**: `EventTicketsController` and `SpaceBookingsController` create the pending record and then a Stripe Checkout Session. They put metadata `Type` (`"Event"` or `"Space"`) and `TicketIds` (comma-separated) or `BookingId` on the session. `StripeWebhookController` (`POST /api/webhook`) handles `checkout.session.completed`. It marks the tickets or booking completed, creates in-app notifications, sends a confirmation email, and records a row in `Payments`. Keep the metadata keys in sync between the creators and the webhook. Success and cancel URLs are built from config `Frontend:BaseUrl`. The webhook signing secret is read from config `Stripe:WebhookSecret`. `EventTicketsController.ConfirmPayment`, called by the success page, re-fetches the Checkout Session from Stripe. It accepts the confirmation only if the session is `paid` and its `TicketIds` metadata covers the requested tickets. The success URLs carry `session_id={CHECKOUT_SESSION_ID}` for this check.
- **Email**: `SaveTicketWithEmailLog` saves the entity and an `ApplicationEmailLogs` row inside a `TransactionScope`, then sends the email through `IEmailService` after the transaction commits. The email HTML templates are inline in the BLL classes.
- **AI chat**: `AiAgentService` builds a Semantic Kernel kernel. It registers the `EventBookingPlugin` and `CoworkingBookingPlugin` (`[KernelFunction]` methods that call the BLL) and uses `AutoInvokeKernelFunctions`. Its system prompt includes the contents of `company-info.txt`, which is read from the working directory. It is a **singleton**: the kernel is built on first use, and chat history is kept in memory for each conversation. The key is `<userId|anon>:<conversationId>`, the client sends a GUID that lasts for the browser tab, history is trimmed to the last 20 messages, and a conversation expires after 30 minutes idle. Because history lives in memory, it is lost on restart and not shared across instances. The request body is `{ message, conversationId }`.
- **Auth**: `UsersController` login issues a JWT with `NameIdentifier` and `Role` claims. The role is normalized to `Admin` for admins, and suspended users get a 403 instead of a token. Controllers use `[Authorize]` / `[Authorize(Roles = Roles.Admin)]`. Endpoints that take a user id or a record owned by a user check `User.CanAccessUser(userId)` (owner or admin), from `CoworkingSpace.API/Security/ClaimsPrincipalExtensions.cs`. Public endpoints are login, register, the event and space read endpoints, booked slots, AI chat, and the Stripe webhook. `Register` honors `RoleId` only when the caller is an admin.
- **Errors**: controllers return `this.ServerError(ex, "message")` (`Infrastructure/ControllerErrorExtensions.cs`), which logs the exception and returns 500 `{ message }`. Never put `ex.Message` in a response. Unhandled exceptions go through `UseExceptionHandler` and return ProblemDetails.
- **Passwords**: `clsPasswordHasher` uses PBKDF2-SHA256 (`PBKDF2$SHA256$<iterations>$<base64>`). Legacy single-SHA-256 hashes still verify, and login re-hashes them (`NeedsRehash`).
- **Frontend auth**: login stores `vantage_token` and `user_data` in localStorage. `interceptors/auth-interceptor.ts` attaches the bearer token and logs the user out on a 401. `authGuard` protects the user pages and `adminGuard` protects `/admin`. These guards only improve the user experience; the API enforces access itself.

## Frontend architecture

- Bootstrapped through `app.config.ts`, with routes in `app.routes.ts`. Folders:
  - `components/`: public pages.
  - `dashboard/`: the user's bookings and tickets.
  - `admin/`: `AdminLayout` with child routes overview, users, spaces, and events.
- `services/` holds one service per backend controller. Each service builds its `apiUrl` from `environment.apiBaseUrl`. `src/environments/environment.ts` (production builds and the Docker image) points to the deployed backend (`https://vantage-api.runasp.net/api`). `environment.development.ts` (used by `ng serve` through `fileReplacements`) points to `http://localhost:8080/api`.
- State uses Angular signals (for example `Auth.currentUser`, `WorkSpaceService.workspaces`).

## Configuration notes

- `appsettings.json` uses the key `defaultConnection` (Windows trusted connection to `.`). Docker overrides it with `ConnectionStrings__DefaultConnection`.
- CORS allows only `Cors:AllowedOrigins` (array; env `Cors__AllowedOrigins__0`, ...), falling back to `Frontend:BaseUrl`. Every deployed frontend origin must be listed there.
- If `OpenAI:ApiKey` is missing, only the chat endpoint fails.
- The backend Dockerfile restores only the API, BLL, and DAL `.csproj` files, then copies everything. If you add a project, add its `.csproj` to the `COPY` lines.
- Files the backend reads at runtime (such as `company-info.txt`) must be copied to the publish output in the `.csproj` and resolved from `AppContext.BaseDirectory`. Under IIS the working directory is not the app folder.

## Deployment

Production runs on free hosting. Azure is no longer used. Deploys are manual; CI only pushes Docker images.

- **Backend + database**: MonsterASP.NET (Windows/IIS, 256 MB RAM) at `https://vantage-api.runasp.net`, with the MSSQL database `db80118` restored from `db-init/CoworkSpaceDB.bak`. Run `dotnet publish CoworkingSpace.API/CoworkingSpace.API.csproj -c Release -o Backend/publish`, then upload `Backend/publish/` to `/wwwroot` over FTPS. Production secrets live in `Backend/publish/appsettings.Production.json`, which is git-ignored and never committed. Keep that file when republishing. CORS origins are read only at startup, so re-upload `web.config` to restart the app after changing them.
- **Frontend**: Netlify at `https://coworking-vantage.netlify.app`, uploaded manually as a zip of `dist/Frontend/browser` (`index.html` at the zip root). `public/_redirects` provides the SPA fallback. Build the zip with forward-slash entry paths: Windows PowerShell 5.1 `Compress-Archive` writes backslashes, and Netlify then serves no images.
- **Stripe** (test mode): the webhook endpoint is `https://vantage-api.runasp.net/api/webhook` for `checkout.session.completed`. Its `whsec_` signing secret goes in `Stripe:WebhookSecret`.
