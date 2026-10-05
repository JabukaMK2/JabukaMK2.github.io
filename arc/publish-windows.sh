#!/usr/bin/env bash
# Publishes ARC, RepairIQ and DataIQ as self-contained Windows builds into one
# folder (ARC launches the other two from next to ARC.exe).
# Usage: ./publish-windows.sh [win-x64|win-arm64|win-x86]
set -euo pipefail
cd "$(dirname "$0")"

RID="${1:-win-x64}"
OUT="dist/$RID"

rm -rf "$OUT"
for app in ARC RepairIQ DataIQ; do
  echo "== Publishing $app ($RID)"
  # Trimming stays off: the three apps share one folder, and a framework DLL
  # trimmed for one app could be missing code another app needs.
  dotnet publish "$app/$app.csproj" -c Release -f net10.0-desktop -r "$RID" \
    --self-contained -p:PublishTrimmed=false -p:PublishReadyToRun=false \
    -p:DebugType=none -o "$OUT"
done

# libSkiaSharp ships a native .pdb that DebugType=none does not cover
find "$OUT" -name "*.pdb" -delete

echo
echo "Done: $OUT  ($(du -sh "$OUT" | cut -f1))"
