#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT_DIR"

dotnet tool restore --tool-manifest src/Game/.config/dotnet-tools.json
dotnet restore
dotnet build

# Content is built via csproj reference
# pushd src/Game/Content >/dev/null
# dotnet mgcb /@Content.mgcb /platform:DesktopGL /output:../bin/Content
# popd >/dev/null

