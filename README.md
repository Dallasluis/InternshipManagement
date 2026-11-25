# Interlane Internship Management Platform

Interlane is a career marketplace and internship management system for students, employers, and administrators. It supports the process from discovering an opportunity through applications, selection, offers, placements, and internship progress.

This repository contains the ASP.NET Core API, the server-rendered web application, shared application/domain code, infrastructure, and tests. For the architecture and implementation walkthrough, see [SYSTEM_DOCUMENTATION.md](SYSTEM_DOCUMENTATION.md). The SRS source review and remaining production work are summarized in [SRS_IMPLEMENTATION_REVIEW.md](SRS_IMPLEMENTATION_REVIEW.md).

## Technology at a glance

- .NET 10 and ASP.NET Core
- ASP.NET Core MVC with Razor views for the web interface
- ASP.NET Core Web API for application data and workflows
- Entity Framework Core 10, SQL Server, and ASP.NET Core Identity
- JWT access tokens and refresh tokens between the web application and API
- SignalR for in-app notification updates
- n8n workflows with Google Gemini for selected AI-assisted features
- SMTP email through MailKit
- Private local document storage by default; optional Azure Blob Storage
- Optional ClamAV-compatible malware scanning for uploaded documents
- xUnit tests, including SQLite-backed document-upload tests

## Requirements

- .NET 10 SDK
- SQL Server LocalDB for the checked-in development connection string
- Docker Desktop only if you want the optional local SMTP test inbox
- A running n8n instance and its required workflows for AI features

## Run locally

### 1. Configure API secrets

The API refuses to start without a JWT signing key and distinct n8n keys. Keep these values in User Secrets during development; do not put them in `appsettings.json` or commit them.

The API project already has a User Secrets ID. Set the values in PowerShell (replace the example values with your own secrets):

```powershell
dotnet user-secrets set "JwtSettings:Key" "<at-least-32-random-bytes>" --project InternshipManagement.Api
dotnet user-secrets set "N8n:OutboundApiKey" "<random-secret-at-least-32-characters>" --project InternshipManagement.Api
dotnet user-secrets set "N8n:InboundApiKey" "<different-random-secret-at-least-32-characters>" --project InternshipManagement.Api
```

The outbound key authenticates API requests to n8n; the inbound key authenticates n8n callbacks to the API. Configure the corresponding credentials in n8n. To create the first administrator, set these once:

```powershell
dotnet user-secrets set "Seed:AdminEmail" "admin@example.com" --project InternshipManagement.Api
dotnet user-secrets set "Seed:AdminPassword" "<strong-development-password>" --project InternshipManagement.Api
```

The administrator is created only if that email does not already exist. If no admin password is configured, API startup logs a warning and does not create an admin account.

### 2. Start the API

In one terminal, from the repository root:

```powershell
dotnet run --project InternshipManagement.Api
```

The development profile serves HTTP on `http://localhost:5003` and HTTPS on `https://localhost:49161`. On startup, the API applies EF Core migrations and creates the Student, Company, and Admin roles. The checked-in development database is SQL Server LocalDB named `InternshipManagementDb`.

### 3. Start the web application

In a second terminal:

```powershell
dotnet run --project InternshipManagement.Web
```

The web application uses `ApiSettings:BaseUrl` from `InternshipManagement.Web/appsettings.json`, currently `http://localhost:5003/`. Change that value for a different local API address.

### 4. Optional local email inbox

The API's Development settings use SMTP at `127.0.0.1:2525`. Start the included SMTP test server with:

```powershell
docker compose -f docker-compose.smtp4dev.yml up -d
```

Open `http://localhost:8080` to view test messages. This is a development mail catcher, not a production mail service.

### 5. Optional AI workflows

The API validates n8n settings at startup, so provide both n8n keys even if you are not exercising AI features. AI requests require n8n to be reachable at `N8n:BaseUrl` and the corresponding workflows to be imported and configured. Workflow JSON files are in `n8n/`; configure Google Gemini credentials in n8n itself. The API enforces application rules and permissions; AI workflows provide generated or streamed responses and do not directly perform hiring actions.

## Configuration notes

ASP.NET Core configuration can be supplied through User Secrets in Development or environment variables in deployment. Nested environment variables use double underscores, for example `ConnectionStrings__DefaultConnection`.

| Setting | Current development behavior |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | SQL Server LocalDB, `InternshipManagementDb` |
| `JwtSettings:Key` | Required secret, at least 32 bytes; never checked into source |
| `JwtSettings:Issuer`, `JwtSettings:Audience` | Checked-in development identifiers; configure deliberately for deployment |
| `ApiSettings:BaseUrl` (Web project) | `http://localhost:5003/` |
| `Email:Host` | `127.0.0.1` in Development; SMTP test server port `2525` |
| `N8n:OutboundApiKey`, `N8n:InboundApiKey` | Required, distinct secrets of at least 32 characters |
| `PrivateDocumentStorage:Provider` | `Local` by default; `AzureBlob` is optional |
| `PrivateDocumentStorage:RootPath` | `App_Data/PrivateDocuments` relative to the API content root |
| `MalwareScanning:Enabled` | Enabled in base settings; explicitly disabled in Development unless a scanner is configured |
| `DocumentRetention:AfterDays` | `null` by default; automatic purging is disabled |
| `Internships:RequireSubscription` | `false` in Development; subscription/billing is outside this system overview |

See [SYSTEM_DOCUMENTATION.md](SYSTEM_DOCUMENTATION.md#configuration-and-local-development) for the Azure, scanner, security, and data-flow details.

## Build and tests

```powershell
dotnet build InternshipManagement.Api/InternshipManagement.Api.csproj
dotnet build InternshipManagement.Web/InternshipManagement.Web.csproj
dotnet test InternshipManagement.UnitTests/InternshipManagement.UnitTests.csproj
dotnet test InternshipManagement.IntegrationTests/InternshipManagement.IntegrationTests.csproj
```

The unit test project has coverage for selected JWT settings, profile fields, ClamAV protocol behavior, and private document upload persistence. The separate IntegrationTests project currently has no substantive workflow tests; a passing test run there does not establish end-to-end coverage.

## Health endpoints

- `GET /health/live` confirms that the API process is responding.
- `GET /health/ready` checks whether the API can connect to its database.

## Repository layout

```text
InternshipManagement.Api/             REST API, auth, controllers, middleware, jobs
InternshipManagement.Web/             MVC/Razor user interface and API clients
InternshipManagement.Application/     Use cases, business workflows, DTOs, interfaces
InternshipManagement.Domain/          Core entities, enums, and domain types
InternshipManagement.Infrastructure/  EF Core, Identity, email, document storage, n8n
InternshipManagement.UnitTests/       Focused automated tests
InternshipManagement.IntegrationTests/Separate integration-test project
n8n/                                  Workflow definitions for AI-assisted features
automation/                           Local automation-related compose/configuration
```

## Current production-readiness caveats

Local development configuration is not a production deployment. Before production, configure managed secrets, database and web/API origins, email delivery, file storage and backups, malware scanning, privacy/access/retention policies, monitoring, and recovery. The optional Azure provider has not been verified against a live Azure account, and the scanner integration has not been exercised against a live ClamAV service. See the SRS review for the broader gap list.
