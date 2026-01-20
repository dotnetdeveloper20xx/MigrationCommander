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
  <a href="#"><img src="https://img.shields.io/badge/License-MIT-blue?style=flat-square" alt="License" /></a>
</p>

<p align="center">
  <a href="#quick-start">Quick Start</a> •
  <a href="#features">Features</a> •
  <a href="#architecture">Architecture</a> •
  <a href="#documentation">Documentation</a> •
  <a href="#roadmap">Roadmap</a>
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
- One of: SQL Server, PostgreSQL, MySQL, or SQLite

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
    options.EnableRealTimeUpdates = true;
    options.InternalDatabasePath = "Data Source=migrationcommander.db";
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
│   ├── MigrationCommander.Data/        # SQLite persistence, repositories
│   ├── MigrationCommander.Providers/   # Database-specific implementations
│   ├── MigrationCommander/             # Main service implementations
│   └── MigrationCommander.Dashboard/   # Blazor Server UI
├── tests/
│   ├── MigrationCommander.Core.Tests/  # 47 unit tests
│   └── MigrationCommander.Integration.Tests/
└── samples/
    └── SampleApp/                      # Example integration
```

### Technology Stack

| Layer | Technology | Why |
|-------|------------|-----|
| **Frontend** | Blazor Server | Real-time updates, C# everywhere, no JS build step |
| **Real-time** | SignalR | Instant migration progress updates |
| **Backend** | .NET 8 | Performance, reliability, LTS support |
| **Persistence** | SQLite + EF Core | Zero external dependencies, portable |
| **Reporting** | QuestPDF + ClosedXML | Professional PDF and Excel generation |
| **Security** | DPAPI + ASP.NET Core | Enterprise-grade encryption and auth |

### Key Design Decisions

1. **Provider Pattern** - Easy to add new database types
2. **Repository Pattern** - Clean data access abstraction
3. **Domain Events** - Decoupled audit logging
4. **Scoped Services** - Proper lifecycle management

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

## Roadmap

### Phase 1: Foundation (Complete)
- [x] Multi-database support (SQL Server, PostgreSQL, MySQL, SQLite)
- [x] Enterprise RBAC with 31 permissions
- [x] Approval workflows with expiration
- [x] Real-time Blazor dashboard
- [x] PDF and Excel reporting
- [x] Comprehensive audit logging

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

## Performance

| Metric | Result |
|--------|--------|
| **Build Time** | ~12 seconds |
| **Test Execution** | 48 tests in <1 second |
| **Dashboard Load** | <500ms |
| **Migration Discovery** | <2 seconds for 100+ migrations |

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
