#!/bin/zsh
set -euo pipefail
project_root="$(cd "$(dirname "$0")/.." && pwd)"
shared_assets="$project_root/../Assets"
output="$project_root/dist/OpenMD.app"
binary_dir="$project_root/.build/release"
swift build -c release --package-path "$project_root"
rm -rf "$output"
mkdir -p "$output/Contents/MacOS" "$output/Contents/Resources"
cp "$binary_dir/OpenMD" "$output/Contents/MacOS/OpenMD"
resource_bundle="$(find "$project_root/.build" -path '*/OpenMDMac_OpenMD.bundle/Contents/Resources/mermaid.min.js' -print -quit)"
if [[ -z "$resource_bundle" ]]; then
  echo "Could not find the SwiftPM Mermaid resource bundle" >&2
  exit 1
fi
cp -R "${resource_bundle%/Contents/Resources/mermaid.min.js}" "$output/Contents/Resources/"
iconset="$(mktemp -d)/OpenMD.iconset"
mkdir "$iconset"
for size in 16 32 128 256 512; do
  sips -z "$size" "$size" "$shared_assets/OpenMD.png" --out "$iconset/icon_${size}x${size}.png" >/dev/null
  double_size=$((size * 2))
  sips -z "$double_size" "$double_size" "$shared_assets/OpenMD.png" --out "$iconset/icon_${size}x${size}@2x.png" >/dev/null
done
iconutil -c icns "$iconset" -o "$output/Contents/Resources/OpenMD.icns"
rm -rf "${iconset:h}"
cat > "$output/Contents/Info.plist" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict><key>CFBundleExecutable</key><string>OpenMD</string><key>CFBundleIconFile</key><string>OpenMD</string><key>CFBundleIdentifier</key><string>com.supersampledlabs.openmd</string><key>CFBundleName</key><string>OpenMD</string><key>CFBundlePackageType</key><string>APPL</string><key>CFBundleShortVersionString</key><string>1.1.0</string><key>LSMinimumSystemVersion</key><string>14.0</string></dict></plist>
PLIST
codesign --force --deep --sign - "$output"
echo "Built $output"
