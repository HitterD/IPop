# IPop

Internal collaboration platform for PT. Santos Jaya Abadi — chat, IM (mail-style), broadcast notification, and extensible to SJA News & Project Management.

## Quick Start

```bash
# Prereq: .NET 8 SDK, Docker Desktop, Node 20+
docker-compose up -d
dotnet build
dotnet ef database update --project src/IPop.Infrastructure --startup-project src/IPop.Host
dotnet run --project src/IPop.Host
```

Open <https://localhost:7001>.

## Architecture

Clean Architecture + Modular Monolith + CQRS. See `docs/superpowers/specs/` for spec, `docs/superpowers/plans/` for implementation plan.
