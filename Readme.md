# DocIntake - Document Intake & Processing Service

A .NET 8 minimal API that accepts legal document submissions, stores them in Azure Blob Storage (emulated locally with Azurite), and processes previews via an in‑memory queue.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Or** Docker + Docker Compose (if you prefer containers)

---

## Running Locally

### Option A — Shell script (simplest)

```bash
chmod +x run.sh
./run.sh
```

The API starts at **http://localhost:5000**  
Swagger UI: **http://localhost:5000/swagger**

### Option B — Docker Compose

```bash
docker compose up --build
```

The API starts at **http://localhost:8080**  
Swagger UI: **http://localhost:8080/swagger**

---

## Running Tests Only

```bash
dotnet test tests/DocIntake.Tests
```
