#!/usr/bin/env bash
set -euo pipefail

dotnet tool restore
dotnet restore
exec dotnet run --project src/Game "$@"

