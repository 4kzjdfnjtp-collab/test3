#!/usr/bin/env bash
set -euo pipefail

RID="${1:-osx-arm64}"
CONFIG="${CONFIG:-Release}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="$ROOT/MIDIProgramSplitter.GUI/MIDIProgramSplitter.GUI.csproj"
OUT="$ROOT/artifacts/$RID/publish"
APP="$ROOT/artifacts/$RID/MIDIProgramSplitter.app"
ZIP="$ROOT/artifacts/MIDIProgramSplitter-$RID.app.zip"

rm -rf "$ROOT/artifacts/$RID"
mkdir -p "$OUT" "$APP/Contents/MacOS" "$APP/Contents/Resources"

dotnet restore "$PROJECT" -r "$RID"
dotnet publish "$PROJECT" \
  -c "$CONFIG" \
  -r "$RID" \
  --self-contained true \
  --no-restore \
  -o "$OUT"

cp -R "$OUT"/. "$APP/Contents/MacOS/"
chmod +x "$APP/Contents/MacOS/MIDIProgramSplitter.GUI" || true

cat > "$APP/Contents/Info.plist" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleDevelopmentRegion</key>
  <string>en</string>
  <key>CFBundleDisplayName</key>
  <string>MIDI Program Splitter</string>
  <key>CFBundleExecutable</key>
  <string>MIDIProgramSplitter.GUI</string>
  <key>CFBundleIdentifier</key>
  <string>com.kermalis.midiprogramsplitter</string>
  <key>CFBundleInfoDictionaryVersion</key>
  <string>6.0</string>
  <key>CFBundleName</key>
  <string>MIDIProgramSplitter</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>CFBundleShortVersionString</key>
  <string>1.0</string>
  <key>CFBundleVersion</key>
  <string>1</string>
  <key>LSMinimumSystemVersion</key>
  <string>11.0</string>
  <key>NSHighResolutionCapable</key>
  <true/>
</dict>
</plist>
PLIST

# Ad-hoc sign so macOS treats the bundle more like a normal local app.
if command -v codesign >/dev/null 2>&1; then
  codesign --force --deep --sign - "$APP"
fi

mkdir -p "$ROOT/artifacts"
rm -f "$ZIP"
/usr/bin/ditto -c -k --sequesterRsrc --keepParent "$APP" "$ZIP"

echo "Created: $ZIP"
