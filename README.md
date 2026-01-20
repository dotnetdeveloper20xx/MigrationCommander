<p align="center">
  <img src="https://img.shields.io/badge/MigrationCommander-Database%20Governance-blue?style=for-the-badge&logo=data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI+PHBhdGggZmlsbD0id2hpdGUiIGQ9Ik0xMiAzQzYuNSAzIDIgNC41IDIgNlYxOEMyIDIwLjUgNi41IDIyIDEyIDIyUzIyIDIwLjUgMjIgMThWNkMyMiA0LjUgMTcuNSAzIDEyIDNNMTIgMTlDNy41OCAxOSA0IDE3Ljc5IDQgMTdWMTQuNzdDNS42MSAxNS41NSA4LjU4IDE2IDEyIDE2UzE4LjM5IDE1LjU1IDIwIDE0Ljc3VjE3QzIwIDE3Ljc5IDE2LjQyIDE5IDEyIDE5TTEyIDEzQzcuNTggMTMgNCAxMi43OSA0IDEyVjkuNzdDNS42MSAxMC41NSA4LjU4IDExIDEyIDExUzE4LjM5IDEwLjU1IDIwIDkuNzdWMTJDMjAgMTIuNzkgMTYuNDIgMTMgMTIgMTNNMTIgOEM3LjU4IDggNCA2Ljc5IDQgNlM3LjU4IDQgMTIgNFMyMCA1LjIxIDIwIDZTMTYuNDIgOCAxMiA4WiIvPjwvc3ZnPg==" alt="MigrationCommander" />
</p>

<h1 align="center">MigrationCommander</h1>

<p align="center">
  <strong>Enterprise Database Change Governance Platform</strong>
</p>

<p align="center">
  <em>Because "YOLO migrations to production" shouldn't be your deployment strategy.</em>
</p>

<p align="center">
  <a href="#features"><img src="https://img.shields.io/badge/Features-20+-green?style=flat-square" alt="Features" /></a>
  <a href="#"><img src="https://img.shields.io/badge/.NET-8.0-purple?style=flat-square&logo=dotnet" alt=".NET 8" /></a>
  <a href="#"><img src="https://img.shields.io/badge/Databases-4%20Types-orange?style=flat-square" alt="4 Database Types" /></a>
  <a href="#"><img src="https://img.shields.io/badge/Tests-48%20Passing-brightgreen?style=flat-square" alt="Tests" /></a>
  <a href="#design-patterns"><img src="https://img.shields.io/badge/Design%20Patterns-6+-blueviolet?style=flat-square" alt="Design Patterns" /></a>
  <a href="#"><img src="https://img.shields.io/badge/License-MIT-blue?style=flat-square" alt="License" /></a>
</p>

<p align="center">
  <a href="#quick-start">Quick Start</a> •
  <a href="#features">Features</a> •
  <a href="#architecture">Architecture</a> •
  <a href="#design-patterns">Design Patterns</a> •
  <a href="#technical-deep-dive">Technical Deep Dive</a>
</p>

---

## The Problem

Every engineering team has that story:

> *"Remember when Dave ran that migration in production without testing it first?"*

Database migrations are the **riskiest part of software deployments**. One bad schema change can:
- Take down production for hours
- Corrupt critical business data
- Result in compliance violations
- Turn your weekend into an incident response marathon

Yet most teams still manage migrations with:
- CLI commands executed by whoever has access
- Zero approval workflows
- Audit trails that live in Slack messages
- "Rollback plans" that are more hope than strategy

## The Solution

**MigrationCommander** is the command center for database changes. It transforms database migrations from a high-risk manual process into a governed, auditable, automated workflow.

```
┌─────────────────────────────────────────────────────────────────────────┐
│                                                                         │
│   Developer writes migration                                            │
│            │                                                            │
│            ▼                                                            │
│   ┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐  │
│   │  MigrationCmd   │───▶│  Preview & Risk  │───▶│    Approval     │  │
│   │   Discovery     │    │    Analysis      │    │    Workflow     │  │
│   └─────────────────┘    └──────────────────┘    └─────────────────┘  │
│                                                           │            │
│                                                           ▼            │
│   ┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐  │
│   │  Complete Audit │◀───│  Safe Execution  │◀───│   Scheduled     │  │
│   │     Trail       │    │   & Rollback     │    │   Deployment    │  │
│   └─────────────────┘    └──────────────────┘    └─────────────────┘  │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Features

### Multi-Database Governance

Manage all your databases from a single pane of glass.

| Database | Status | Features |
|----------|--------|----------|
| **SQL Server** | Full Support | All features including native DDL generation |
| **PostgreSQL** | Full Support | Full migration management with PG-specific syntax |
| **MySQL** | Full Support | Complete governance workflows |
| **SQLite** | Full Support | Perfect for development and testing |

### Enterprise Security & RBAC

**31 granular permissions** organized into intuitive roles:

| Role | Access Level | Use Case |
|------|--------------|----------|
| **Admin** | Full system control | Platform administrators |
| **DBA** | Full migration access | Database team leads |
| **Developer** | Non-production deployment | Development teams |
| **Viewer** | Read-only audit access | Compliance reviewers |

### Approval Workflows

*No production changes slip through unreviewed.*

- **Environment-aware rules** - Different approval requirements per environment
- **Expiring approvals** - No stale approvals sitting in the queue
- **Audit integration** - Complete chain of custody for every change
- **Multi-approver support** - Require sign-off from multiple stakeholders

### Real-Time Dashboard

A beautiful Blazor Server dashboard with **SignalR-powered live updates**:

- Live migration progress tracking
- Environment health at a glance
- Pending approvals and scheduled migrations
- Comprehensive audit log with advanced filtering

### Advanced Scheduling

*Migrations that deploy when you want them to.*

- Schedule migrations for off-peak hours
- Automatic execution with full logging
- Cancellation with reason tracking
- Failure notifications and auto-retry options

### Comprehensive Reporting

**Export-ready compliance evidence:**

| Format | Use Case |
|--------|----------|
| **PDF Reports** | Executive summaries, audit submissions |
| **Excel Exports** | Detailed analysis, custom filtering |
| **JSON/CSV** | System integrations, data processing |

### SQL Preview & Impact Analysis

*See exactly what will happen before it happens.*

- Full DDL preview for any migration
- Affected tables and estimated row counts
- Risk indicators for destructive operations
- Rollback script generation

---

## Quick Start

### Prerequisites

- .NET 8.0 SDK
- **No Docker required** - Uses in-memory SQLite by default

### Installation

```bash
# Clone the repository
git clone https://github.com/dotnetdeveloper20xx/MigrationCommander.git
cd MigrationCommander

# Build the solution
dotnet build

# Run the dashboard
dotnet run --project src/MigrationCommander.Dashboard
```

### First Steps

1. **Navigate to** `https://localhost:5001`
2. **Add your first environment** - Click "Environments" → "Add Environment"
3. **Discover migrations** - MigrationCommander will find your EF Core migrations
4. **Preview and apply** - See exactly what each migration will do

### Integration with Your Project

```csharp
// In your Program.cs or Startup.cs
builder.Services.AddMigrationCommander(options =>
{
    // In-memory database - no external dependencies
    options.InternalDatabasePath = "Data Source=MigrationCommander;Mode=Memory;Cache=Shared";
    options.EnableRealTimeUpdates = true;
});

// In the middleware pipeline
app.UseMigrationCommander(seedTestData: true); // Remove seedTestData for production
```

---

## Architecture

MigrationCommander follows **Clean Architecture** principles with clear separation of concerns:

```
MigrationCommander/
├── src/
│   ├── MigrationCommander.Core/        # Domain models, interfaces, business logic
│   │   ├── Interfaces/                 # 14+ focused interfaces (ISP compliant)
│   │   ├── Models/                     # Rich domain models with behavior
│   │   └── Services/                   # Core business logic
│   │
│   ├── MigrationCommander.Data/        # Persistence layer
│   │   ├── Entities/                   # Database entities (separate from domain)
│   │   ├── Repositories/               # Repository pattern implementations
│   │   └── Configurations/             # EF Core Fluent API configurations
│   │
│   ├── MigrationCommander.Providers/   # Database-specific implementations
│   │   ├── SqlServer/                  # SQL Server provider
│   │   ├── PostgreSQL/                 # PostgreSQL provider
│   │   ├── MySQL/                      # MySQL provider
│   │   └── SQLite/                     # SQLite provider
│   │
│   ├── MigrationCommander/             # Application services
│   │   ├── Services/                   # Service implementations
│   │   ├── BackgroundServices/         # Hosted services
│   │   └── Extensions/                 # DI registration
│   │
│   └── MigrationCommander.Dashboard/   # Blazor Server UI
│       ├── Components/                 # Razor components
│       ├── Hubs/                       # SignalR hubs
│       └── Services/                   # UI-specific services
│
├── tests/
│   ├── MigrationCommander.Core.Tests/  # 48 unit tests
│   └── MigrationCommander.Integration.Tests/
│
└── samples/
    └── SampleApp/                      # Example integration
```

### Technology Stack

| Layer | Technology | Why |
|-------|------------|-----|
| **Frontend** | Blazor Server | Real-time updates, C# everywhere, no JS build step |
| **Real-time** | SignalR | WebSocket-based instant updates with group subscriptions |
| **Backend** | .NET 8 | Performance, reliability, LTS support |
| **Persistence** | SQLite + EF Core | Zero external dependencies, in-memory option |
| **Reporting** | QuestPDF + ClosedXML | Professional PDF and Excel generation |
| **Security** | DPAPI + ASP.NET Core | Enterprise-grade encryption and auth |

---

## Design Patterns

MigrationCommander demonstrates mastery of enterprise design patterns:

### Factory Pattern

The `MigrationProviderFactory` dynamically creates database-specific providers:

```csharp
public class MigrationProviderFactory : IMigrationProviderFactory
{
    public IMigrationProvider CreateProvider(ProviderType providerType)
    {
        return providerType switch
        {
            ProviderType.SqlServer => _serviceProvider.GetRequiredService<SqlServerMigrationProvider>(),
            ProviderType.PostgreSQL => _serviceProvider.GetRequiredService<PostgreSqlMigrationProvider>(),
            ProviderType.MySQL => _serviceProvider.GetRequiredService<MySqlMigrationProvider>(),
            ProviderType.SQLite => _serviceProvider.GetRequiredService<SqliteMigrationProvider>(),
            _ => throw new NotSupportedException($"Provider type {providerType} is not supported")
        };
    }
}
```

### Strategy Pattern

Each database provider implements `BaseMigrationProvider` with provider-specific strategies:

```csharp
// Base abstraction
public abstract class BaseMigrationProvider : IMigrationProvider
{
    public abstract Task<IReadOnlyList<MigrationInfo>> GetPendingMigrationsAsync();
    public abstract Task<MigrationResult> ApplyMigrationAsync(string migrationId);
    protected abstract string GetMigrationHistoryQuery();
}

// SQL Server Strategy
public class SqlServerMigrationProvider : BaseMigrationProvider
{
    protected override string GetMigrationHistoryQuery() =>
        "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId";
}

// PostgreSQL Strategy - different syntax
public class PostgreSqlMigrationProvider : BaseMigrationProvider
{
    protected override string GetMigrationHistoryQuery() =>
        "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\"";
}
```

### Observer Pattern

Event-driven migration execution with real-time notifications:

```csharp
public class MigrationExecutorService : IMigrationExecutor
{
    public event EventHandler<MigrationProgressEventArgs>? ProgressChanged;
    public event EventHandler<MigrationCompletedEventArgs>? MigrationCompleted;
    public event EventHandler<MigrationFailedEventArgs>? MigrationFailed;

    private void OnProgressChanged(string migrationId, int percentComplete, string status)
    {
        ProgressChanged?.Invoke(this, new MigrationProgressEventArgs
        {
            MigrationId = migrationId,
            PercentComplete = percentComplete,
            Status = status
        });
    }
}
```

### Repository Pattern

Clean data access abstraction with domain/entity separation:

```csharp
public class DatabaseRepository
{
    private readonly MigrationCommanderDbContext _context;
    private readonly IDataProtector _protector;

    public async Task<ConfiguredDatabase?> GetByIdAsync(Guid id)
    {
        var entity = await _context.ConfiguredDatabases.FindAsync(id);
        return entity != null ? MapToDomain(entity) : null;
    }

    // Domain model mapping - keeps entities internal
    private ConfiguredDatabase MapToDomain(ConfiguredDatabaseEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        ConnectionString = _protector.Unprotect(entity.EncryptedConnectionString),
        ProviderType = Enum.Parse<ProviderType>(entity.ProviderType)
    };
}
```

### Builder Pattern

Fluent service configuration:

```csharp
builder.Services.AddMigrationCommander(options =>
{
    options.InternalDatabasePath = "Data Source=MigrationCommander;Mode=Memory;Cache=Shared";
    options.EnableRealTimeUpdates = true;
    options.ApplicationName = "MigrationCommander";
    options.DataProtectionKeyPath = "/keys";
});
```

### Decorator Pattern

SignalR notifier decorates the null notifier for real-time updates:

```csharp
// Base implementation (NullMigrationNotifier)
public class NullMigrationNotifier : IMigrationNotifier
{
    public Task NotifyProgressAsync(Guid environmentId, MigrationProgress progress)
        => Task.CompletedTask;
}

// SignalR decorator adds real functionality
public class SignalRMigrationNotifier : IMigrationNotifier
{
    private readonly IHubContext<MigrationHub> _hubContext;

    public async Task NotifyProgressAsync(Guid environmentId, MigrationProgress progress)
    {
        await _hubContext.Clients
            .Group($"env-{environmentId}")
            .SendAsync("MigrationProgress", progress);
    }
}
```

---

## Technical Deep Dive

### SOLID Principles Implementation

#### Single Responsibility Principle (SRP)
Each service has one clear purpose:
- `MigrationDiscoveryService` - Only discovers migrations
- `MigrationExecutorService` - Only executes migrations
- `AuditLogService` - Only handles audit logging
- `SqlPreviewGenerator` - Only generates SQL previews

#### Open/Closed Principle (OCP)
New database providers can be added without modifying existing code:
```csharp
// Just create a new provider class
public class OracleMigrationProvider : BaseMigrationProvider
{
    // Implement abstract methods
}
// Register in DI container
services.AddScoped<OracleMigrationProvider>();
```

#### Interface Segregation Principle (ISP)
14+ focused interfaces instead of one large interface:
```csharp
public interface IMigrationDiscovery { ... }      // Discovery only
public interface IMigrationExecutor { ... }       // Execution only
public interface IAuditLogger { ... }             // Audit only
public interface IApprovalWorkflow { ... }        // Approvals only
public interface IMigrationScheduler { ... }      // Scheduling only
public interface IRollbackManager { ... }         // Rollback only
public interface IStatisticsService { ... }       // Statistics only
public interface IReportGenerator { ... }         // Reports only
public interface IAuthorizationService { ... }    // Auth only
// ... and more
```

#### Dependency Inversion Principle (DIP)
All services depend on abstractions:
```csharp
public class MigrationExecutorService : IMigrationExecutor
{
    private readonly IMigrationProviderFactory _providerFactory;  // Abstraction
    private readonly IAuditLogger _auditLogger;                   // Abstraction
    private readonly IMigrationNotifier _notifier;                // Abstraction

    public MigrationExecutorService(
        IMigrationProviderFactory providerFactory,
        IAuditLogger auditLogger,
        IMigrationNotifier notifier)
    {
        _providerFactory = providerFactory;
        _auditLogger = auditLogger;
        _notifier = notifier;
    }
}
```

### SignalR Real-Time Architecture

Group-based subscriptions for efficient broadcasting:

```csharp
public class MigrationHub : Hub
{
    public async Task JoinEnvironmentGroup(Guid environmentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"env-{environmentId}");
    }

    public async Task LeaveEnvironmentGroup(Guid environmentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"env-{environmentId}");
    }
}

// Broadcasting to specific environments
await _hubContext.Clients
    .Group($"env-{environmentId}")
    .SendAsync("MigrationProgress", new
    {
        MigrationId = migrationId,
        Status = "Applying",
        PercentComplete = 50
    });
```

### Security Architecture

#### Connection String Encryption (DPAPI)
```csharp
public class DatabaseRepository
{
    private readonly IDataProtector _protector;

    public async Task AddAsync(ConfiguredDatabase database)
    {
        var entity = new ConfiguredDatabaseEntity
        {
            EncryptedConnectionString = _protector.Protect(database.ConnectionString)
        };
        await _context.SaveChangesAsync();
    }
}
```

#### Permission-Based Authorization
```csharp
public class AuthorizationService : IAuthorizationService
{
    public async Task<bool> HasPermissionAsync(string userId, Permission permission)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user?.HasPermission(permission) ?? false;
    }
}

// In Blazor components
@if (await AuthorizationService.HasPermissionAsync(userId, Permission.ApplyMigrations))
{
    <button @onclick="ApplyMigration">Apply</button>
}
```

### Entity Framework Configuration

Fluent API configuration with proper indexes and relationships:

```csharp
public class MigrationCommanderDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Composite index for efficient queries
        modelBuilder.Entity<MigrationHistoryEntity>()
            .HasIndex(e => new { e.DatabaseId, e.MigrationId })
            .IsUnique();

        // Environment-based queries
        modelBuilder.Entity<AuditLogEntity>()
            .HasIndex(e => new { e.EnvironmentId, e.Timestamp });

        // User lookup optimization
        modelBuilder.Entity<UserEntity>()
            .HasIndex(e => e.Username)
            .IsUnique();
    }
}
```

### Background Service Pattern

Scheduled migration worker using `IHostedService`:

```csharp
public class ScheduledMigrationWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var scheduler = scope.ServiceProvider.GetRequiredService<IMigrationScheduler>();

            var dueMigrations = await scheduler.GetDueMigrationsAsync();
            foreach (var migration in dueMigrations)
            {
                await ExecuteScheduledMigrationAsync(migration, scope);
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
```

### Domain Model with Behavior

Rich domain models instead of anemic data containers:

```csharp
public class User
{
    public string Id { get; set; }
    public string Username { get; set; }
    public IReadOnlyList<Role> Roles { get; set; }

    public bool HasPermission(Permission permission)
    {
        return Roles.Any(r => r.Permissions.Contains(permission));
    }

    public bool CanApproveFor(string requesterId)
    {
        // Business rule: Can't approve own requests
        return Id != requesterId && HasPermission(Permission.ApproveProduction);
    }
}

public class ExecutionResult
{
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
    public TimeSpan Duration { get; private set; }

    public static ExecutionResult Succeeded(TimeSpan duration)
        => new() { Success = true, Duration = duration };

    public static ExecutionResult Failed(string error, TimeSpan duration)
        => new() { Success = false, ErrorMessage = error, Duration = duration };
}
```

---

## Test Data

MigrationCommander ships with comprehensive seed data for testing:

| Entity | Count | Description |
|--------|-------|-------------|
| **Environments** | 6 | Dev, QA, Staging, Production (US/EU/Asia) |
| **Users** | 6 | Admin, DBA, Developers, Viewer |
| **Migrations** | 150+ | Realistic history across environments |
| **Audit Logs** | 200+ | 30 days of activity |
| **Scheduled** | 7 | Various statuses |
| **Approvals** | 7 | Pending, approved, rejected, expired |

---

## Performance

| Metric | Result |
|--------|--------|
| **Build Time** | ~12 seconds |
| **Test Execution** | 48 tests in <1 second |
| **Dashboard Load** | <500ms |
| **Migration Discovery** | <2 seconds for 100+ migrations |
| **Memory Footprint** | ~50MB (in-memory SQLite) |

---

## Roadmap

### Phase 1: Foundation (Complete)
- [x] Multi-database support (SQL Server, PostgreSQL, MySQL, SQLite)
- [x] Enterprise RBAC with 31 permissions
- [x] Approval workflows with expiration
- [x] Real-time Blazor dashboard
- [x] PDF and Excel reporting
- [x] Comprehensive audit logging
- [x] In-memory database support

### Phase 2: Intelligence (In Progress)
- [ ] Migration risk scoring
- [ ] Schema drift detection
- [ ] Predictive failure analysis
- [ ] Smart scheduling recommendations

### Phase 3: Integration
- [ ] Azure DevOps pipeline tasks
- [ ] GitHub Actions
- [ ] REST API with API key auth
- [ ] Slack/Teams notifications

### Phase 4: Scale
- [ ] Multi-tenant SaaS option
- [ ] SSO/SAML authentication
- [ ] Custom plugin architecture
- [ ] AI-powered migration generation

---

## Code Quality

| Metric | Value |
|--------|-------|
| **Design Patterns** | 6+ (Factory, Strategy, Observer, Repository, Builder, Decorator) |
| **SOLID Compliance** | All 5 principles |
| **Interfaces** | 14+ focused interfaces |
| **Test Coverage** | 48 unit tests |
| **Warnings** | 2 minor (nullable reference) |
| **External Dependencies** | Minimal (no Docker required) |

---

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## Support

- **Issues**: [GitHub Issues](https://github.com/dotnetdeveloper20xx/MigrationCommander/issues)
- **Discussions**: [GitHub Discussions](https://github.com/dotnetdeveloper20xx/MigrationCommander/discussions)

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## Acknowledgments

Built with passion and caffeine by developers who've been woken up at 3 AM by bad migrations one too many times.

---

<p align="center">
  <strong>Stop hoping your migrations work. Start knowing they will.</strong>
</p>

<p align="center">
  <a href="https://github.com/dotnetdeveloper20xx/MigrationCommander">
    <img src="https://img.shields.io/github/stars/dotnetdeveloper20xx/MigrationCommander?style=social" alt="GitHub Stars" />
  </a>
</p>
