$ErrorActionPreference = "Stop"

$RootDir = Split-Path -Parent $PSScriptRoot
$Project = Join-Path $RootDir "Xabe.FFmpeg/Xabe.FFmpeg.csproj"
$OutputDir = if ($args.Length -gt 0) { $args[0] } else { Join-Path $RootDir "artifacts/nuget" }

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
dotnet pack $Project -c Release -o $OutputDir
