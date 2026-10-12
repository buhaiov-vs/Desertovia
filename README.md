# Desertod

Audience village game for streamers. Game rules: [docs/game-design.md](docs/game-design.md). Progress and plans: [docs/roadmap.md](docs/roadmap.md).

## Structure

| Path | Content |
|---|---|
| `src/Desertod.Domain` | Game rules: stats, roles, tasks catalog, progression, energy, cycle engine. No dependencies. |
| `src/Desertod.Infrastructure` | Entity Framework Core + PostgreSQL, account linking, migrations. |
| `src/Desertod.Api` | ASP.NET Core: authentication, HTTP endpoints, SignalR hub, background cycle settlement. |
| `tests/Desertod.Domain.Tests` | Unit tests for the game math. |
| `web` | React + PixiJS frontend (Vite). Production build goes to `src/Desertod.Api/wwwroot`. |

## Run locally

Prerequisites: .NET 10 SDK, Node.js 24, Docker.

```powershell
docker compose up -d
dotnet run --project src/Desertod.Api            # http://localhost:5080, applies migrations in Development
cd web; npm install; npm run dev                 # http://localhost:5190, proxies /api, /auth, /hubs to the backend
```

Open http://localhost:5190. In Development a name-only login is available for testing.

Pages:

- `/` — sign in, list your villages, browse open villages.
- `/new` — streamers found a village: name, optional description, open or invite-only. The owner of an invite-only village finds the invite link and code in Settings and can replace the code; `/v/{slug}?invite={code}` fills it in for viewers.
- `/v/{slug}` — citizen page: create a character, pick tasks, renew every 90 seconds.
- `/v/{slug}/overlay` — transparent village view for a browser source in streaming software (no login needed).

## Login providers

Secrets are stored with user secrets (never in `appsettings.json`). Providers without configuration are hidden.

```powershell
cd src/Desertod.Api
dotnet user-secrets set "Authentication:Twitch:ClientId" "<id>"
dotnet user-secrets set "Authentication:Twitch:ClientSecret" "<secret>"
dotnet user-secrets set "Authentication:Google:ClientId" "<id>"
dotnet user-secrets set "Authentication:Google:ClientSecret" "<secret>"
dotnet user-secrets set "Authentication:Telegram:BotUsername" "<bot username without @>"
dotnet user-secrets set "Authentication:Telegram:BotToken" "<token from BotFather>"
```

| Provider | Where to register | Redirect / domain |
|---|---|---|
| Twitch | https://dev.twitch.tv/console/apps | `http://localhost:5190/signin-twitch` |
| YouTube (Google sign-in) | Google Cloud Console → Credentials → OAuth client ID (web application) | `http://localhost:5190/signin-google` |
| Telegram | BotFather: create a bot, then `/setdomain` | Telegram does not accept `localhost`; use a public domain or a tunnel |

Notes:

- YouTube login is Google sign-in; it identifies the person, not their channel membership. Membership checks need the YouTube Data API (not implemented yet).
- Twitch subscription checks are not implemented yet either: currently any signed-in account can join any village.

## Languages

English is the default; Russian is also available. Players switch with the language selector in the top bar (the choice is saved in the browser). The stream overlay accepts `?lang=ru`.

- All player-facing text lives in `web/src/i18n`. `en.ts` is the source of truth; other dictionaries are typed against it, so a missing key is a compile error.
- The server stays language-neutral: catalog names are English, and rule errors return a `code` plus `arguments` in the problem details (`detail` is the English fallback). The client translates task, role, project and resource names by identifier.
- To add a language: create `web/src/i18n/<code>.ts` typed as `Dictionary`, then register it in `DICTIONARIES` and `LANGUAGES` in `translator.ts`.

## Database migrations

```powershell
dotnet tool restore
dotnet ef migrations add <MigrationName> --project src/Desertod.Infrastructure --startup-project src/Desertod.Api --output-dir Data/Migrations
```

## Tests

| Suite | What it covers | Needs |
|---|---|---|
| `tests/Desertod.Domain.Tests` | Game math. | — |
| `tests/Desertod.Api.Tests` | API smoke tests: the real app on a throwaway PostgreSQL container — public endpoints, login, founding a village, joining as a citizen, starting a task, the SignalR hub. | Docker |
| `web/e2e` | Browser smoke tests (Playwright, desktop and phone): every page, every menu tab, the inventory, the overlay popup, login and character creation; fails on any uncaught script error. | `docker compose up -d` |

```powershell
dotnet test Desertod.slnx

cd web
npx playwright install chromium   # once
npm run test:e2e                  # or npm run test:e2e:ui to watch them run
```

The browser tests start their own backend (port 5181, database `desertod_e2e`, created automatically) and a production build of the client under `vite preview` (port 5191), so a running development setup is left alone. Set `DESERTOD_E2E_CONNECTION` to use another database. Failures leave screenshots and traces in `web/test-results`; `npx playwright show-report` opens the report.

While `dotnet run` holds `src/Desertod.Api/bin`, build the .NET tests elsewhere: `dotnet test Desertod.slnx --artifacts-path $env:TEMP\desertod-test-build`.
