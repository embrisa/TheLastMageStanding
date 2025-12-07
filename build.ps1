#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"

Set-Location -Path $PSScriptRoot

dotnet tool restore --tool-manifest src/Game/.config/dotnet-tools.json
dotnet restore
dotnet build

Push-Location src/Game
dotnet mgcb /@Content.mgcb /platform:DesktopGL /output:bin/Content
Pop-Location

