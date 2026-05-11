#!/usr/bin/env bash
set -euo pipefail

echo "══════════════════════════════════════════"
echo "  DocIntake – Local Run Script"
echo "══════════════════════════════════════════"

# Verify .NET 8 SDK is available
if ! dotnet --version | grep -q "^8\."; then
  echo "ERROR: .NET 8 SDK is required. Install from https://dotnet.microsoft.com/download"
  exit 1
fi

echo "▶ Restoring packages..."
dotnet restore

echo "▶ Running unit tests..."
dotnet test DocIntake.Tests --configuration Release --logger "console;verbosity=normal"

echo "▶ Starting API on http://localhost:5000 ..."
echo "   Swagger UI: http://localhost:5000/swagger"
echo "   Press Ctrl+C to stop."
echo ""

export ASPNETCORE_ENVIRONMENT=Development
export Storage__LocalPath="./doc-storage"

dotnet run --project DocIntake.Api --configuration Release --urls "http://localhost:5000"
