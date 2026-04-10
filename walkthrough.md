# Authentication & Authorization System — Walkthrough

## What Was Built

A production-ready, multi-tenant Authentication & Authorization system integrated into `Tharwat.Easy.Notifications` using **ASP.NET Core Identity + JWT** with an **Ocelot API Gateway**. Both projects compile successfully.

---

## Architecture

```
                            ┌─────────────────────────────────┐
[ Browser / Mobile / CRM ]  │   API Gateway (Ocelot)          │  :5002
         │                  │   • JWT validation              │
         └─────────────────►│   • Per-tenant rate limiting    │
                            │   • Structured request logging  │
                            │   • Route → downstream forward  │
                            └───────────────┬─────────────────┘
                                            │
                            ┌───────────────▼─────────────────┐
                            │   Notification API              │  :5000
                            │   • /api/auth/*   (no auth)     │
                            │   • /api/users/*  (Admin only)  │
                            │   • /api/clients/* (clients.manage)
                            │   • /api/notifications/* (scope)│
                            └───────────────┬─────────────────┘
                                            │
                            ┌───────────────▼─────────────────┐
                            │         SQL Server              │
                            │  dbo.*       → Notification DB  │
                            │  identity.*  → Identity DB      │
                            └─────────────────────────────────┘
```

---

## Files Created

### Infrastructure — Identity Layer
| File | Purpose |
|------|---------|
| `Identity/Entities/ApplicationUser.cs` | Extends `IdentityUser<Guid>` — platform operators with email+password login |
| `Identity/Entities/ApplicationRole.cs` | Extends `IdentityRole<Guid>` — tenant-scoped roles |
| `Identity/Entities/UserTenant.cs` | Many-to-many: user ↔ tenant with per-tenant role |
| `Identity/Entities/Client.cs` | Third-party M2M API client (Client Credentials flow) |
| `Identity/Entities/ClientScope.cs` | OAuth scopes granted to a client |
| `Identity/Entities/RefreshToken.cs` | Rotation-based refresh tokens for both flows |
| `Identity/Data/ApplicationIdentityDbContext.cs` | Separate `IdentityDbContext` on `identity` schema |
| `Identity/Data/IdentityDbContextFactory.cs` | Design-time factory for EF migrations |
| `Identity/Scopes/ScopeConstants.cs` | Central scope string registry |
| `Identity/Services/RoleScopeMapper.cs` | Maps role names → scope strings at token time |
| `Identity/Services/ITokenService.cs` + `TokenService.cs` | JWT generation, rotation, revocation |
| `Identity/Services/IClientSecretService.cs` + `ClientSecretService.cs` | BCrypt hashing, secret generation |
| `Identity/Services/IdentitySeedService.cs` | Idempotent super-admin + system role seed |
| `Identity/Services/TokenResponse.cs` | Token endpoint response DTO |

### API Layer — Auth System
| File | Purpose |
|------|---------|
| `Authorization/Requirements/ScopeRequirement.cs` | `IAuthorizationRequirement` + `IAuthorizationHandler` |
| `Middleware/TenantContext.cs` | `ITenantContext` + `TenantContext` scoped service |
| `Middleware/TenantResolutionMiddleware.cs` | Extracts `tenant_id` from JWT → `ITenantContext` |
| `Models/Auth/AuthModels.cs` | All auth DTOs (Login, Refresh, Client, User, etc.) |
| `Controllers/AuthController.cs` | Login, ClientToken, Refresh, Logout |
| `Controllers/ClientManagementController.cs` | Provision/rotate/revoke API clients |
| `Controllers/UserManagementController.cs` | Register/assign/deactivate users per tenant |
| `IdentityDependencyInjection.cs` | Central DI wiring for the entire auth system |

### Modified Files
| File | Change |
|------|--------|
| `Program.cs` | Added `AddIdentityServices`, correct middleware order, seed + migration |
| `appsettings.json` | Added `Jwt` + `Identity` sections |
| `Infrastructure.csproj` | Added Identity EF Core, BCrypt, IdentityModel 8.14.0 |
| `API.csproj` | Added JwtBearer 9.0.3 |

### Gateway (New Project)
| File | Purpose |
|------|---------|
| `Gateway.csproj` | Ocelot 23.4.2 + JwtBearer + Serilog |
| `Program.cs` | Ocelot + JWT auth + custom middlewares pipeline |
| `appsettings.json` | Shared JWT config + rate limit config |
| `ocelot.json` | Full route table: per-route auth, rate limits, QoS |
| `Middleware/TenantRateLimitMiddleware.cs` | Sliding-window rate limiter per tenant/client/IP |
| `Middleware/GatewayLoggingMiddleware.cs` | Structured request logging with correlation IDs |

---

## JWT Token Structure

### User Token
```json
{
  "sub":        "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email":      "admin@tenant.com",
  "full_name":  "Tenant Admin",
  "tenant_id":  "11111111-1111-1111-1111-111111111111",
  "token_type": "user",
  "scope":      "notifications.send",
  "scope":      "templates.manage",
  "scope":      "analytics.read",
  "role":       "Admin",
  "jti":        "unique-id",
  "exp":        1234567890
}
```

### Client Credentials Token
```json
{
  "sub":        "aaaabbbb-cccc-dddd-eeee-ffffgggghhhh",
  "client_id":  "cln_AbCdEfGhIjKlMnOp",
  "tenant_id":  "11111111-1111-1111-1111-111111111111",
  "token_type": "client_credentials",
  "scope":      "notifications.send",
  "jti":        "unique-id",
  "exp":        1234567890
}
```

---

## API Endpoints

### Auth (`/api/auth`)
| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `POST` | `/api/auth/login` | None | Email + Password → JWT |
| `POST` | `/api/auth/client-token` | None | ClientId + ClientSecret → JWT |
| `POST` | `/api/auth/refresh` | None | Rotate refresh token |
| `POST` | `/api/auth/logout` | Bearer | Revoke refresh token |

### Clients (`/api/clients`) — requires `clients.manage` scope
| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/clients` | Provision new client (secret shown once) |
| `GET` | `/api/clients` | List tenant clients |
| `GET` | `/api/clients/{id}` | Get client details |
| `PUT` | `/api/clients/{id}/rotate-secret` | Rotate secret (old tokens revoked) |
| `DELETE` | `/api/clients/{id}` | Deactivate client |

### Users (`/api/users`) — requires `Admin` or `SuperAdmin` role
| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/users` | Register user in current tenant |
| `GET` | `/api/users` | List users in current tenant |
| `GET` | `/api/users/{id}` | Get user details |
| `PUT` | `/api/users/{id}/roles` | Change user's tenant role |
| `POST` | `/api/users/{id}/tenants` | Add user to another tenant |
| `DELETE` | `/api/users/{id}` | Deactivate user |

---

## Authorization Policies

| Policy | Type | Requirement |
|--------|------|-------------|
| `RequireSuperAdmin` | Role | `SuperAdmin` |
| `RequireAdmin` | Role | `Admin` OR `SuperAdmin` |
| `RequireManager` | Role | `Admin` OR `Manager` OR `SuperAdmin` |
| `CanSendNotifications` | Scope | `notifications.send` |
| `CanReadNotifications` | Scope | `notifications.read` OR `notifications.send` |
| `CanManageTemplates` | Scope | `templates.manage` |
| `CanReadTemplates` | Scope | `templates.read` OR `templates.manage` |
| `CanManageTenants` | Scope | `tenants.manage` |
| `CanManageClients` | Scope | `clients.manage` |
| `CanReadAnalytics` | Scope | `analytics.read` |

> **Wildcard**: SuperAdmin tokens carry the `*` scope, which bypasses all scope checks automatically.

### Usage in Controllers
```csharp
[Authorize(Policy = "CanSendNotifications")]
[HttpPost("send")]
public async Task<IActionResult> Send([FromBody] SendNotificationRequest request) { ... }

[Authorize(Policy = "RequireAdmin")]
[HttpGet("reports")]
public async Task<IActionResult> Reports() { ... }
```

---

## Identity Database Schema (`identity.*`)

```
identity.Users           → ApplicationUser (extends AspNetUsers)
identity.Roles           → ApplicationRole (extends AspNetRoles)
identity.UserRoles       → IdentityUserRole<Guid>
identity.UserClaims      → IdentityUserClaim<Guid>
identity.UserLogins      → IdentityUserLogin<Guid>
identity.UserTokens      → IdentityUserToken<Guid>
identity.RoleClaims      → IdentityRoleClaim<Guid>
identity.UserTenants     → UserTenant  (composite PK: UserId + TenantId)
identity.Clients         → Client      (unique index on ClientId)
identity.ClientScopes    → ClientScope (unique index on ClientId + Scope)
identity.RefreshTokens   → RefreshToken (unique index on Token)
```

---

## ⚙️ EF Core Migrations — Run These Commands

> Run from the **solution root** (`d:\osama\easy.notifications`).

### Create the Identity migration
```powershell
dotnet ef migrations add InitIdentity `
  --context ApplicationIdentityDbContext `
  --project Tharwat.Easy.Notifications.Infrastructure `
  --startup-project Tharwat.Easy.Notifications.API `
  --output-dir Identity/Migrations
```

### Apply the migration
```powershell
dotnet ef database update `
  --context ApplicationIdentityDbContext `
  --project Tharwat.Easy.Notifications.Infrastructure `
  --startup-project Tharwat.Easy.Notifications.API
```

> The existing `NotificationDbContext` migrations are **unaffected** — they continue to run independently at startup as before.

---

## Ocelot Gateway Configuration

| Route | Port | Auth | Rate Limit |
|-------|------|------|------------|
| `/api/auth/**` | 5000 | None | 30 req/min |
| `/api/notifications/**` | 5000 | Bearer | 500 req/min + QoS |
| `/api/templates/**` | 5000 | Bearer | 200 req/min + QoS |
| `/api/clients/**` | 5000 | Bearer | 100 req/min |
| `/api/users/**` | 5000 | Bearer | 100 req/min |
| `/api/channels/**` | 5000 | Bearer | 100 req/min |
| `/api/tenants/**` | 5000 | Bearer | 50 req/min |
| `/api/analytics/**` | 5000 | Bearer | 200 req/min |

**Identity-aware rate limiting** (TenantRateLimitMiddleware):
- Authenticated users → partitioned by `tenant_id` (1 000 req/min)
- API clients → partitioned by `client_id` (100 req/min)
- Anonymous → partitioned by IP (20 req/min)

---

## Quickstart: Test the Auth Flow

### 1. Login as Super Admin
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "superadmin@easynotifications.com",
    "password": "SuperAdmin@2024!",
    "tenantId": "00000000-0000-0000-0001-000000000001"
  }'
```

### 2. Use the access token
```bash
curl http://localhost:5000/api/users \
  -H "Authorization: Bearer <access_token>"
```

### 3. Create an API client
```bash
curl -X POST http://localhost:5000/api/clients \
  -H "Authorization: Bearer <access_token>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "CRM Integration",
    "scopes": ["notifications.send", "notifications.read"]
  }'
# → Response contains clientId + clientSecret (save the secret — shown once!)
```

### 4. Get a client credentials token
```bash
curl -X POST http://localhost:5000/api/auth/client-token \
  -H "Content-Type: application/json" \
  -d '{
    "clientId": "cln_...",
    "clientSecret": "..."
  }'
```

### 5. Send a notification using the client token
```bash
curl -X POST http://localhost:5000/api/notifications \
  -H "Authorization: Bearer <client_access_token>" \
  -H "Content-Type: application/json" \
  -d '{ ... }'
```

---

## 🔐 Production Security Checklist

> [!CAUTION]
> Before deploying to production, complete every item on this list.

- [ ] **Replace `Jwt:SecretKey`** in `appsettings.json` with a cryptographically random 64-char secret. Store it in **Azure Key Vault** or environment variables — never in source control.
- [ ] **Change super-admin password** from the default `SuperAdmin@2024!` via the `Identity:SuperAdmin:Password` config key.
- [ ] **Enable HTTPS** — uncomment `app.UseHttpsRedirection()` in `Program.cs`.
- [ ] **Set `RequireConfirmedEmail = true`** in `IdentityDependencyInjection.cs` once an email service is wired up.
- [ ] **Enable Hangfire authentication** — add `[Authorize]` or IP filter to the `/admin` dashboard.
- [ ] **Tune rate limits** in `Gateway/appsettings.json` to match your expected traffic.
- [ ] **Replace in-memory rate limiter** with a Redis-backed store for multi-instance Gateway deployments.
- [ ] **Add token denylist** (Redis) if immediate access-token invalidation on logout is required (currently only refresh tokens are revoked).
- [ ] **Configure CORS** — replace `*` in `Gateway/appsettings.json → Cors:AllowedOrigins` with your actual front-end domains.
- [ ] **Run EF migration** (commands above) before first deployment.
- [ ] **Update `ocelot.json`** downstream host/port values from `localhost:5000` to your production service addresses/DNS names.

---

## How to Add Auth to an Existing Controller

```csharp
// Require a specific scope:
[Authorize(Policy = "CanSendNotifications")]
[HttpPost("send")]
public async Task<IActionResult> Send([FromBody] SendRequest req)
{
    // Access the current tenant anywhere via DI:
    var tenantId = _tenantContext.TenantId;
    ...
}

// Require a role:
[Authorize(Policy = "RequireAdmin")]
[HttpDelete("{id}")]
public async Task<IActionResult> Delete(Guid id) { ... }

// Allow any authenticated user:
[Authorize]
[HttpGet]
public async Task<IActionResult> Get() { ... }

// Allow anonymous (login, health, etc.):
[AllowAnonymous]
[HttpGet("ping")]
public IActionResult Ping() => Ok("pong");
```
