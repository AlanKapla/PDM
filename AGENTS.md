# AGENTS.md — PDM (Project Data Management)

## Repo structure

```
PDM/
├── 01-Applications/
│   ├── ProjectDataManagementUI/     # Main SPA (React 18 + Chakra UI 2 + Vite 7)
│   └── BricklyLandingPage/          # Marketing landing (lighter React + Vite 6)
├── 02-ApplicationServices/
│   └── ProductDataManagementWebAPI/ # .NET 10 solution, 12 projects
│       └── src/
│           ├── WebApi/              # Controllers, Hubs (SignalR), Middleware
│           ├── CQRS/                # Commands, Queries, Handlers, Behaviours
│           ├── Business/            # Domain services, interfaces, web models
│           ├── Entities/            # EF Core DbContext, entities, migrations
│           ├── Repositories/        # IRepository<T>, IReadRepository<T>
│           ├── Chat/                # Chat module
│           ├── Business.AIAgent/    # AI agent integration
│           └── FileUpload/          # File handling
├── 03-Deployment/                   # Docker Compose, nginx, Dockerfiles
├── opencode.json                    # OpenCode project config
├── AGENTS.md                        # This file
├── .opencode/
│   ├── agents/                      # Agent definitions (auto-discovered)
│   ├── skills/                      # **Canonical coding patterns — read before implementing**
│   │   ├── api-cqrs/SKILL.md
│   │   ├── api-controllers/SKILL.md
│   │   ├── api-validators/SKILL.md
│   │   ├── api-entities/SKILL.md
│   │   ├── api-repositories/SKILL.md
│   │   ├── api-services/SKILL.md
│   │   ├── api-unit-tests/SKILL.md
│   │   ├── ui-components/SKILL.md
│   │   ├── ui-hooks/SKILL.md
│   │   ├── ui-types/SKILL.md
│   │   ├── ui-theme/SKILL.md
│   │   ├── ui-api-client/SKILL.md
│   │   ├── ui-forms-modals/SKILL.md
│   │   ├── ui-unit-tests/SKILL.md
│   │   ├── ui-accessibility/SKILL.md
│   │   └── brickly-landing/SKILL.md
│   ├── features/                    # Feature specifications (4)
│   ├── subagents/rules/             # Subagent execution rules
│   └── testCases/                   # Manual/generated test scenarios
└── .github/
    ├── workflows/
    │   ├── deploy.yml               # Self-hosted Docker deploy (dev/ppd/prd)
    │   └── azure-deploy.yml         # Azure App Service + Static Web Apps
    └── copilot-instructions.md      # Existing instructions (kept, detailed)
```

## Key commands

### UI (ProjectDataManagementUI)
| Command | What |
|---|---|
| `npm run dev` | Vite dev server (port 5173) |
| `npm run build` | `tsc -b && vite build` — type-check before build |
| `npm run lint` | ESLint |
| `npm test` | Vitest (watch mode) |
| `npm run test:run` | Vitest single run |
| `npm run test:axe` | AXE accessibility tests only (`src/**/*.axe.test.*`) |

### API (ProductDataManagementWebAPI)
```powershell
# From solution dir (02-ApplicationServices/ProductDataManagementWebAPI)
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release --no-build
# Single project:
dotnet test tests/CQRS.Tests
dotnet test tests/Business.Tests
dotnet test tests/WebApi.Tests
```

### EF Core migrations
```powershell
cd src/Entities
dotnet ef migrations add <Name> --startup-project ../WebApi
dotnet ef migrations script --idempotent --output migration.sql --startup-project ../WebApi
```
CI pin: `dotnet-ef` version `10.0.1`, **not** latest.

## CQRS pipeline (order matters)
1. `ValidationBehavior` — FluentValidation
2. `AuthorizationBehavior` — `IAuthorizableRequest.PermissionCode`
3. `AssignedAuthorizationBehavior` — `IAssignedAuthorizableRequest`
4. `TransactionBehavior` — EF Core transaction for Commands

## API conventions
- **No `var`** — always explicit type
- `is null` / `is not null` (never `== null`)
- `{}` on every block
- Max ~20 lines per method
- Handlers `sealed`
- `IReadRepository<T>` for reads, `IRepository<T>` for writes
- Predicates always include `TenantId` + `ProjectId`
- Domain exceptions: `NotFoundApiException`, `ForbiddenApiException`, `ConflictApiException` (not `InvalidOperationException`)
- Auth: Azure AD B2C + JWT Bearer; access_token also accepted from query string (for SignalR)

## UI conventions
- **No `any`** — always explicit type
- Logic in hooks, components only render
- Colors via Chakra tokens or `appColors` from `theme/tokens/colors.ts`
- No inline styles
- `AppModal` instead of custom modal implementations
- One file = one component
- `interface {Component}Props` for props, return `React.ReactElement`
- Named exports for domain components, default exports for `components/ui/`
- Clickable table row pattern: `cursor="pointer"` + `_hover` on `<Tr>`, `e.stopPropagation()` on action buttons

## Testing quirks
- **API**: xUnit + FluentAssertions + Moq (3 test projects). `dotnet test` at solution root.
- **UI**: Vitest (config inline in `vite.config.ts`), jsdom, setup in `src/test/setup.ts`
  - Use `renderWithChakra` from `src/test/render-with-chakra.tsx` for Chakra-aware renders
  - `vitest-axe` matchers (`toHaveNoViolations`) registered globally in setup
  - `window.matchMedia` mocked globally for Chakra UI
  - Accessibility tests: `npm run test:axe`

## Docker & deployment
- Three stacks: webapi (.NET 10, port 8080), ui (nginx-served SPA, port 80), nginx (reverse proxy, config in `03-Deployment/`)
- Dev: `docker compose -f docker-compose.yml -f docker-compose.development.yml up -d` (gateway on port 8085)
- Prod: docker compose project named `pdm-{environment}`; container names: `pdm-webapi-{env}`, `pdm-ui-{env}`, `pdm-nginx-{env}`
- API Dockerfile builds with `-r linux-x64 --no-self-contained` — Linux target cross-compiled from any host
- nginx: 50MB upload limit, WebSocket support for `/api/hubs/`, long timeouts (3600s for WS)
- Health check: `GET /api/health`
- Redis is optional with graceful degradation (no crash if disabled)
- API configures Kestrel with 50MB request body limit + 5min timeouts; also `FormOptions`

## SignalR hubs
| Path | Purpose |
|---|---|
| `/api/hubs/notifications` | Real-time notifications |
| `/api/hubs/messages` | Chat messages |
| `/api/hubs/ai` | AI assistant |

## Environment files
- `.env.example` → copy to `.env.local` for local dev
- `.env` (default, `VITE_API_BASE_URL=/api`)
- `.env.demo`, `.env.local` (gitignored)
- API env vars: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_CLIENT_SECRET`, `CONNECTIONSTRINGS__DEFAULTCONNECTION`

## Swagger
Available only in Development: `https://localhost:{port}/swagger`. The UI **skips React mount** on `/swagger` paths.

## Skills
Before implementing **any** new feature, read the relevant skill in `.opencode/skills/`. These are the canonical pattern references — the rest of the repo may have older code that doesn't reflect current conventions.

## OpenCode config
`opencode.json` at repo root. Skills auto-discovered from `.opencode/skills/`. Agents auto-discovered from `.opencode/agents/`. After any config change, restart OpenCode to reload.
