# Authentication & Authorization System — Full Implementation Plan

## Overview

This plan integrates a **production-ready, multi-tenant Authentication & Authorization system** into the existing `Tharwat.Easy.Notifications` codebase. The system uses **ASP.NET Core Identity + JWT** for user and client authentication, **YARP** (replacing Ocelot) as an API Gateway, scope + role-based authorization, and a dedicated `Identity` project to keep concerns separated.

The existing codebase is **.NET 9**, uses **Entity Framework Core 9** with **SQL Server**, follows **Clean Architecture**, and has an existing `Tenant` and `User` domain entity. The plan **co-exists** with the existing `NotificationDbContext` by introducing a **separate `IdentityDbContext`** that shares the same SQL Server database.

---

## Architecture Diagram

```
[ Client / 3rd Party App ]
          │
          ▼
┌──────────────────────────────────────┐
│  API Gateway  (YARP)                 │
│  - JWT Validation                    │
│  - Rate Limiting (per tenant/client) │
│  - Request Routing                   │
│  - Logging                           │
└───────────────┬──────────────────────┘
                │
    ┌───────────┴────────────┐
    ▼                        ▼
[ Identity Service ]   [ Notification API ]
[ /api/auth/...    ]   [ /api/notifications/...]
    │                        │
    └──────────┬─────────────┘
               ▼
         [ SQL Server ]
     ┌──────────────────────┐
     │  IdentityDbContext   │  ← ASP.NET Identity tables
     │  NotificationDbContext│  ← Existing domain tables
     └──────────────────────┘
```

---

## User Review Required

> [!IMPORTANT]
> **Separation Strategy**: A new project `Tharwat.Easy.Notifications.Identity` will be created to host all Identity logic (entities, DbContext, token service). The existing `NotificationDbContext` is **not modified** — it continues with the existing `Tenant` and `User` domain entities. The Identity `ApplicationUser` is the **authentication identity only**; it links to `TenantId` but does not replace the domain `User`.

> [!WARNING]
> **Existing `User` entity**: Your existing domain `User` entity is a notification-targeting user (recipient), NOT an authentication user. The new `ApplicationUser` (Identity) is the platform operator/admin user. Do NOT merge them. The `ApplicationUser.ExternalUserId` can optionally link to the domain `User`.

> [!IMPORTANT]
> **API Gateway Project**: A new `Tharwat.Easy.Notifications.Gateway` project will be created using **YARP** (not Ocelot). YARP is Microsoft-maintained, actively developed, and integrates natively with ASP.NET Core middleware.

> [!CAUTION]
> **Migration impact**: New Identity migrations will be created in the Identity project. They operate on the **same database** as the existing migrations but use a separate schema (`identity`). Run both migrations independently.

---

## Proposed Changes

### Component 1: Identity Domain Entities

#### [NEW] `Tharwat.Easy.Notifications.Identity` Project

New class library project with all Identity-related entities and services.

---

#### [NEW] `Identity/Entities/ApplicationUser.cs`

Extends `IdentityUser<Guid>`. Adds multi-tenant fields:

```csharp
public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    // Navigation: user can belong to multiple tenants
    public ICollection<UserTenant> UserTenants { get; set; }
}
```

---

#### [NEW] `Identity/Entities/ApplicationRole.cs`

Extends `IdentityRole<Guid>`. Scoped per-tenant roles:

```csharp
public class ApplicationRole : IdentityRole<Guid>
{
    public Guid TenantId { get; set; }       // role is scoped to a tenant
    public string? Description { get; set; }
}
```

---

#### [NEW] `Identity/Entities/UserTenant.cs`

Many-to-many mapping: a user can be member of multiple tenants with a tenant-specific role:

```csharp
public class UserTenant
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public Guid RoleId { get; set; }         // role within THIS tenant
    public bool IsDefault { get; set; }
    public DateTime JoinedAt { get; set; }
    public ApplicationUser User { get; set; }
    public ApplicationRole Role { get; set; }
}
```

---

#### [NEW] `Identity/Entities/Client.cs`

Third-party API clients (machine-to-machine):

```csharp
public class Client
{
    public Guid Id { get; set; }
    public string ClientId { get; set; }          // public identifier
    public string ClientSecretHash { get; set; }  // bcrypt hashed
    public string Name { get; set; }
    public Guid TenantId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<ClientScope> ClientScopes { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; }
}
```

---

#### [NEW] `Identity/Entities/ClientScope.cs`

```csharp
public class ClientScope
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public string Scope { get; set; }  // e.g. "notifications.send"
    public Client Client { get; set; }
}
```

---

#### [NEW] `Identity/Entities/RefreshToken.cs`

```csharp
public class RefreshToken
{
    public Guid Id { get; set; }
    public string Token { get; set; }        // stored as hash
    public Guid? UserId { get; set; }
    public Guid? ClientId { get; set; }
    public Guid TenantId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRevoked { get; set; }
    public string? ReplacedByToken { get; set; }
    public ApplicationUser? User { get; set; }
    public Client? Client { get; set; }
}
```

---

### Component 2: Identity DbContext

#### [NEW] `Identity/Data/ApplicationIdentityDbContext.cs`

```csharp
public class ApplicationIdentityDbContext 
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public DbSet<UserTenant> UserTenants { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<ClientScope> ClientScopes { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("identity");
        // UserTenant composite PK: (UserId, TenantId)
        // Unique index on Client.ClientId
        // Index on RefreshToken.Token
    }
}
```

---

### Component 3: JWT Token Service

#### [NEW] `Identity/Services/TokenService.cs`

Handles both **user tokens** and **client credential tokens**:

```csharp
// User JWT claims:
{
  "sub": "user-guid",
  "email": "user@example.com",
  "tenant_id": "tenant-guid",
  "roles": ["Admin", "Manager"],
  "scopes": ["notifications.send", "templates.manage"],
  "client_id": null,
  "token_type": "user",
  "jti": "unique-token-id",
  "exp": 1234567890
}

// Client JWT claims:
{
  "sub": "client-guid",
  "client_id": "client-id-string",
  "tenant_id": "tenant-guid",
  "scopes": ["notifications.send"],
  "token_type": "client_credentials",
  "jti": "unique-token-id",
  "exp": 1234567890
}
```

Methods:
- `GenerateUserTokenAsync(ApplicationUser user, Guid tenantId)` → `TokenResponse`
- `GenerateClientTokenAsync(Client client)` → `TokenResponse`  
- `GenerateRefreshToken()` → `string`
- `ValidateRefreshToken(string token, Guid? userId, Guid? clientId)` → `RefreshToken?`

Configuration (`appsettings.json`):
```json
"Jwt": {
  "SecretKey": "your-256-bit-secret-key-here...",
  "Issuer": "easy-notifications-auth",
  "Audience": "easy-notifications-api",
  "AccessTokenExpirationMinutes": 60,
  "RefreshTokenExpirationDays": 30
}
```

---

### Component 4: Authentication Controllers

#### [NEW] `Identity/Controllers/AuthController.cs`

Endpoints:

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/auth/login` | Email + Password → JWT + Refresh |
| `POST` | `/api/auth/refresh` | Refresh token → new JWT pair |
| `POST` | `/api/auth/logout` | Revoke refresh token |
| `POST` | `/api/auth/client-token` | client_id + client_secret → JWT |

**Login Request:**
```json
{
  "email": "admin@tenant.com",
  "password": "P@ssword123",
  "tenantId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Login Response:**
```json
{
  "accessToken": "eyJ...",
  "refreshToken": "abc...",
  "expiresIn": 3600,
  "tokenType": "Bearer"
}
```

**Client Credentials Request:**
```json
{
  "clientId": "my-crm-app",
  "clientSecret": "raw-secret-here",
  "tenantId": "3fa85f64-..."
}
```

---

#### [NEW] `Identity/Controllers/ClientManagementController.cs`

Admin-only. Manages third-party clients:

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/clients` | Create client → returns `clientId` + `clientSecret` (shown once) |
| `GET`  | `/api/clients` | List tenant clients |
| `PUT`  | `/api/clients/{id}/rotate-secret` | Rotate client secret |
| `DELETE` | `/api/clients/{id}` | Revoke client |

---

#### [NEW] `Identity/Controllers/UsersController.cs`

Tenant-scoped user management:

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/users` | Register user |
| `GET`  | `/api/users` | List users in tenant |
| `PUT`  | `/api/users/{id}/roles` | Assign roles |
| `POST` | `/api/users/{id}/tenants` | Assign user to another tenant |

---

### Component 5: Authorization System

#### [NEW] `Identity/Authorization/Scopes/ScopeConstants.cs`

```csharp
public static class Scopes
{
    public const string NotificationsSend   = "notifications.send";
    public const string NotificationsRead   = "notifications.read";
    public const string TemplatesManage     = "templates.manage";
    public const string TemplatesRead       = "templates.read";
    public const string TenantsManage       = "tenants.manage";
    public const string ClientsManage       = "clients.manage";
    public const string AnalyticsRead       = "analytics.read";
}
```

---

#### [NEW] `Identity/Authorization/Requirements/ScopeRequirement.cs`

```csharp
public class ScopeRequirement : IAuthorizationRequirement
{
    public string RequiredScope { get; }
    public ScopeRequirement(string scope) => RequiredScope = scope;
}

public class ScopeAuthorizationHandler 
    : AuthorizationHandler<ScopeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        ScopeRequirement requirement)
    {
        var scopes = context.User.FindAll("scopes")
            .SelectMany(c => c.Value.Split(' '));
        
        if (scopes.Contains(requirement.RequiredScope))
            context.Succeed(requirement);
        
        return Task.CompletedTask;
    }
}
```

---

#### [NEW] `Identity/Authorization/Requirements/TenantRequirement.cs`

```csharp
// Ensures user's token tenant_id matches the requested resource's tenant
public class TenantAuthorizationHandler 
    : AuthorizationHandler<TenantRequirement, ITenantResource>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantRequirement requirement,
        ITenantResource resource)
    {
        var tokenTenantId = context.User.FindFirstValue("tenant_id");
        if (tokenTenantId == resource.TenantId.ToString())
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
```

---

#### [MODIFY] `API/DependencyInjection.cs` — Add Authorization Policies

```csharp
services.AddAuthorization(options =>
{
    // Role-based
    options.AddPolicy("RequireAdmin", p => 
        p.RequireRole("Admin"));
    options.AddPolicy("RequireManager", p => 
        p.RequireRole("Admin", "Manager"));

    // Scope-based
    options.AddPolicy("CanSendNotifications", p => 
        p.AddRequirements(new ScopeRequirement(Scopes.NotificationsSend)));
    options.AddPolicy("CanManageTemplates", p => 
        p.AddRequirements(new ScopeRequirement(Scopes.TemplatesManage)));
    options.AddPolicy("CanManageTenants", p => 
        p.AddRequirements(new ScopeRequirement(Scopes.TenantsManage)));
    options.AddPolicy("CanManageClients", p =>
        p.AddRequirements(new ScopeRequirement(Scopes.ClientsManage)));
});
```

---

### Component 6: Tenant Resolution Middleware

#### [NEW] `Identity/Middleware/TenantResolutionMiddleware.cs`

Resolves the current tenant from the JWT `tenant_id` claim on every request. Populates `ITenantContext` (scoped service) so downstream services can access it.

```csharp
public interface ITenantContext
{
    Guid TenantId { get; }
    bool IsResolved { get; }
}

// Middleware reads "tenant_id" claim → validates tenant exists → 
// sets ITenantContext in DI container
```

---

### Component 7: API Gateway (YARP)

#### [NEW] `Tharwat.Easy.Notifications.Gateway` Project

New ASP.NET Core Web project using **YARP** (Microsoft.ReverseProxy).

##### Project structure:
```
Gateway/
├── Program.cs
├── appsettings.json          ← YARP routes + clusters
├── Middleware/
│   ├── JwtValidationMiddleware.cs
│   ├── RateLimitMiddleware.cs
│   └── GatewayLoggingMiddleware.cs
├── RateLimiting/
│   └── TenantRateLimitPolicy.cs
└── Tharwat.Easy.Notifications.Gateway.csproj
```

##### YARP Route Configuration (`appsettings.json`):

```json
{
  "ReverseProxy": {
    "Routes": {
      "auth-route": {
        "ClusterId": "identity-cluster",
        "Match": { "Path": "/api/auth/{**catch-all}" }
      },
      "notifications-route": {
        "ClusterId": "notifications-cluster",
        "AuthorizationPolicy": "authenticated",
        "Match": { "Path": "/api/notifications/{**catch-all}" }
      },
      "templates-route": {
        "ClusterId": "notifications-cluster",
        "AuthorizationPolicy": "CanManageTemplates",
        "Match": { "Path": "/api/templates/{**catch-all}" }
      }
    },
    "Clusters": {
      "identity-cluster": {
        "Destinations": {
          "dest1": { "Address": "http://localhost:5001/" }
        }
      },
      "notifications-cluster": {
        "Destinations": {
          "dest1": { "Address": "http://localhost:5000/" }
        }
      }
    }
  }
}
```

##### Rate Limiting (per tenant + per client):

Uses `System.Threading.RateLimiting` (built-in .NET 7+):

```csharp
// Sliding window: 1000 req/min per tenant, 100 req/min per client
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("tenant-policy", context =>
    {
        var tenantId = context.User?.FindFirst("tenant_id")?.Value ?? "anonymous";
        return RateLimitPartition.GetSlidingWindowLimiter(tenantId, _ => 
            new SlidingWindowRateLimiterOptions 
            { 
                PermitLimit = 1000, 
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 4
            });
    });
    
    options.AddPolicy("client-policy", context =>
    {
        var clientId = context.User?.FindFirst("client_id")?.Value ?? "anonymous";
        return RateLimitPartition.GetSlidingWindowLimiter(clientId, _ =>
            new SlidingWindowRateLimiterOptions 
            { 
                PermitLimit = 100, 
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 4
            });
    });
});
```

---

### Component 8: JWT Validation in `Program.cs`

#### [MODIFY] `API/Program.cs`

Add JWT bearer authentication setup:

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!)),
            ClockSkew = TimeSpan.Zero
        };
        
        // Extract tenant context from JWT on token validation
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var tenantCtx = ctx.HttpContext.RequestServices
                    .GetRequiredService<ITenantContext>();
                // populate tenant context
            }
        };
    });

// Pipeline order (CRITICAL):
app.UseAuthentication();  // BEFORE UseAuthorization
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();
```

---

### Component 9: Internal Service-to-Service Authentication

For RabbitMQ workers and internal HTTP calls, use a **shared secret header** validated via custom middleware, or a short-lived service account JWT:

#### [NEW] `Identity/Services/InternalServiceTokenService.cs`

```csharp
// Issues short-lived (5min) service JWTs with audience "internal"
// No refresh tokens for service tokens
// Configuration:
// "InternalService": { "SharedSecret": "...", "TokenExpiry": 5 }
```

#### [NEW] `Identity/Middleware/InternalServiceAuthMiddleware.cs`

```csharp
// Validates X-Service-Token header on internal endpoints
// Alternatively: use the same JWT validation with audience="internal"
```

---

## Files to Create

| File | Location |
|------|----------|
| `ApplicationUser.cs` | `Identity/Entities/` |
| `ApplicationRole.cs` | `Identity/Entities/` |
| `UserTenant.cs` | `Identity/Entities/` |
| `Client.cs` | `Identity/Entities/` |
| `ClientScope.cs` | `Identity/Entities/` |
| `RefreshToken.cs` | `Identity/Entities/` |
| `ApplicationIdentityDbContext.cs` | `Identity/Data/` |
| `IdentityDbContextFactory.cs` | `Identity/Data/` |
| `TokenService.cs` | `Identity/Services/` |
| `TokenResponse.cs` | `Identity/Services/` |
| `ClientSecretService.cs` | `Identity/Services/` |
| `InternalServiceTokenService.cs` | `Identity/Services/` |
| `ScopeConstants.cs` | `Identity/Authorization/Scopes/` |
| `ScopeRequirement.cs` | `Identity/Authorization/Requirements/` |
| `TenantRequirement.cs` | `Identity/Authorization/Requirements/` |
| `TenantResolutionMiddleware.cs` | `Identity/Middleware/` |
| `ITenantContext.cs` | `Identity/Middleware/` |
| `AuthController.cs` | `Identity/Controllers/` |
| `ClientManagementController.cs` | `Identity/Controllers/` |
| `UsersController.cs` | `Identity/Controllers/` |
| `IdentityDependencyInjection.cs` | `Identity/` |
| `Program.cs` (Gateway) | `Gateway/` |
| `appsettings.json` (Gateway) | `Gateway/` |
| `JwtValidationMiddleware.cs` | `Gateway/Middleware/` |
| `RateLimitMiddleware.cs` | `Gateway/Middleware/` |
| `GatewayLoggingMiddleware.cs` | `Gateway/Middleware/` |

## Files to Modify

| File | Change |
|------|--------|
| `API/Program.cs` | Add `AddAuthentication`, `UseAuthentication`, `UseAuthorization` |
| `API/DependencyInjection.cs` | Add authorization policies, `ITenantContext` |
| `API/appsettings.json` | Add `Jwt` section |
| `Infrastructure/NotificationDbContext.cs` | No changes needed |
| `Tharwat.Easy.Notifications.API.csproj` | Add JWT NuGet packages |
| `Tharwat.Easy.Notifications.sln` | Add new Identity and Gateway projects |

---

## NuGet Packages Required

### Identity Project
```
Microsoft.AspNetCore.Identity.EntityFrameworkCore
Microsoft.EntityFrameworkCore.SqlServer
Microsoft.IdentityModel.Tokens
System.IdentityModel.Tokens.Jwt
BCrypt.Net-Next
```

### API Project (additions)
```
Microsoft.AspNetCore.Authentication.JwtBearer
```

### Gateway Project
```
Microsoft.ReverseProxy (YARP)
Microsoft.AspNetCore.Authentication.JwtBearer
System.Threading.RateLimiting (built-in .NET 7+)
```

---

## Verification Plan

### Automated Tests
- `dotnet build` — ensure zero compile errors across all projects
- Manual smoke tests via Swagger / cURL:
  - `POST /api/auth/login` → expect 200 + access/refresh tokens
  - `POST /api/auth/client-token` → expect 200 with `token_type: client_credentials`
  - `GET /api/notifications` without token → expect 401
  - `GET /api/notifications` with token missing scope → expect 403
  - `GET /api/notifications` with valid token + scope → expect 200
  - Rate limit test: 101 rapid requests via client → expect 429

### Manual Verification
- Decode JWT at [jwt.io](https://jwt.io) and verify claims: `sub`, `tenant_id`, `roles`, `scopes`, `client_id`
- Verify `identity` schema tables in SQL Server Management Studio
- Test YARP Gateway routes (localhost:5002) correctly forward to backend services

---

## Open Questions

> [!IMPORTANT]
> **Q1**: Should the new **Identity project** be hosted as a **separate microservice** (its own port/deployment), or embedded inside the existing API project? Separate = cleaner, but requires an additional service to run.

> [!IMPORTANT]
> **Q2**: Should the **Gateway** be added to the existing solution, or is it a separate repository? Currently, the `docker-*.yml` files only show one API container.

> [!NOTE]
> **Q3**: Do you want password complexity rules configured (e.g., minimum length, special chars)? ASP.NET Identity has built-in options for this.

> [!NOTE]
> **Q4**: Should `ApplicationUser` be seeded with a default **super-admin** user on first startup?
