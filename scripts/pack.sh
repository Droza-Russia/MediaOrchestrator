#!/bin/sh
set -eu

ROOT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
PROJECT="$ROOT_DIR/MediaOrchestrator/MediaOrchestrator.csproj"
OUTPUT_DIR="${1:-$ROOT_DIR/artifacts/nuget}"

mkdir -p "$OUTPUT_DIR"
dotnet pack "$PROJECT" -c Release -o "$OUTPUT_DIR"
