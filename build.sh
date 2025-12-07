#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT_DIR"

dotnet tool restore --tool-manifest src/Game/.config/dotnet-tools.json
dotnet restore
dotnet build

pushd src/Game >/dev/null
dotnet mgcb /@Content.mgcb /platform:DesktopGL /output:bin/Content
popd >/dev/null

