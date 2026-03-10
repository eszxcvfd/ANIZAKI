# Backend Command Surface

This document is the canonical command surface for backend bootstrap and verification.

## Prerequisites
- .NET SDK 8.x installed

## Canonical Commands

### Restore
```powershell
dotnet restore src/api/Anizaki.Api.sln
```

### Build
```powershell
dotnet build src/api/Anizaki.Api.sln
```

### Test
```powershell
dotnet test src/api/Anizaki.Api.sln
```

## Quick Verification Loop
```powershell
dotnet restore src/api/Anizaki.Api.sln
dotnet build src/api/Anizaki.Api.sln
dotnet test src/api/Anizaki.Api.sln
```

## Docker (Backend Only)

Build image:
```powershell
docker build -f src/api/Dockerfile -t anizaki-api:local src/api
```

Run container on local port 5080:
```powershell
docker run --rm -p 5080:8080 `
  -e ConnectionStrings__MongoDb="mongodb+srv://admin:<db_password>@cluster0.3vhtrmn.mongodb.net/?appName=Cluster0" `
  anizaki-api:local
```

## Project Layout
- `src/api/src/Anizaki.Domain`
- `src/api/src/Anizaki.Application`
- `src/api/src/Anizaki.Infrastructure`
- `src/api/src/Anizaki.Api`
- `src/api/tests/*`
