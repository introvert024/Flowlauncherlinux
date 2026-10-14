#!/bin/bash
# Build AppImage for universal Linux distribution
set -e
VERSION=${FLOW_VERSION:-"1.0.0"}
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
PUBLISH_DIR="$ROOT_DIR/output/publish/linux-x64"
APPDIR="$ROOT_DIR/output/FlowLauncher.AppDir"

# Download appimagetool if not present
if [ ! -f /tmp/appimagetool ]; then
    echo "Downloading appimagetool..."
    wget -O /tmp/appimagetool https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage || \
    curl -Lo /tmp/appimagetool https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage
    chmod +x /tmp/appimagetool
fi

rm -rf "$APPDIR"
mkdir -p "$APPDIR/usr/bin" "$APPDIR/usr/lib/flow-launcher" "$APPDIR/usr/share/applications" "$APPDIR/usr/share/icons/hicolor/256x256/apps"

if [ ! -d "$PUBLISH_DIR" ]; then
    echo "Publish directory $PUBLISH_DIR not found. Running build..."
    if [ -f "$ROOT_DIR/scripts/build.sh" ]; then
        bash "$ROOT_DIR/scripts/build.sh"
    fi
fi

cp -r "$PUBLISH_DIR"/* "$APPDIR/usr/lib/flow-launcher/"

cat > "$APPDIR/usr/bin/flow-launcher" << 'SCRIPT'
#!/bin/bash
BIN_DIR="$(dirname "$(readlink -f "$0")")"
LIB_DIR="$BIN_DIR/../lib/flow-launcher"
if [ -x "$LIB_DIR/flow-launcher" ]; then
    exec "$LIB_DIR/flow-launcher" "$@"
elif [ -x "$LIB_DIR/FlowLauncher.Linux" ]; then
    exec "$LIB_DIR/FlowLauncher.Linux" "$@"
else
    exec dotnet "$LIB_DIR/FlowLauncher.Linux.dll" "$@"
fi
SCRIPT
chmod +x "$APPDIR/usr/bin/flow-launcher"

# Copy desktop entry
if [ -f "$SCRIPT_DIR/flow-launcher.desktop" ]; then
    cp "$SCRIPT_DIR/flow-launcher.desktop" "$APPDIR/usr/share/applications/"
    cp "$SCRIPT_DIR/flow-launcher.desktop" "$APPDIR/"
fi

# Copy icon if available
if [ -f "$SCRIPT_DIR/flow-launcher.png" ]; then
    cp "$SCRIPT_DIR/flow-launcher.png" "$APPDIR/usr/share/icons/hicolor/256x256/apps/flow-launcher.png"
    cp "$SCRIPT_DIR/flow-launcher.png" "$APPDIR/flow-launcher.png"
    cp "$SCRIPT_DIR/flow-launcher.png" "$APPDIR/.DirIcon"
elif [ -f "$ROOT_DIR/assets/flow-launcher.png" ]; then
    cp "$ROOT_DIR/assets/flow-launcher.png" "$APPDIR/usr/share/icons/hicolor/256x256/apps/flow-launcher.png"
    cp "$ROOT_DIR/assets/flow-launcher.png" "$APPDIR/flow-launcher.png"
    cp "$ROOT_DIR/assets/flow-launcher.png" "$APPDIR/.DirIcon"
fi

# AppRun
cat > "$APPDIR/AppRun" << 'APPRUN'
#!/bin/bash
HERE="$(dirname "$(readlink -f "${0}")")"
exec "$HERE/usr/bin/flow-launcher" "$@"
APPRUN
chmod +x "$APPDIR/AppRun"

OUTPUT_FILE="$ROOT_DIR/output/FlowLauncher-$VERSION-x86_64.AppImage"
mkdir -p "$ROOT_DIR/output"
ARCH=x86_64 /tmp/appimagetool "$APPDIR" "$OUTPUT_FILE"
echo "AppImage created: $OUTPUT_FILE"
