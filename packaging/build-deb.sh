#!/bin/bash
# Build Debian package for Flow Launcher Linux
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
OUTPUT_DIR="$ROOT_DIR/output"
PUBLISH_DIR="$OUTPUT_DIR/publish/linux-x64"

# 1. Determine version from environment variable, git tag, or csproj
if [ -n "$FLOW_VERSION" ]; then
    VERSION="$FLOW_VERSION"
elif git describe --tags --always >/dev/null 2>&1; then
    VERSION="$(git describe --tags --always | sed 's/^v//')"
elif [ -f "$ROOT_DIR/src/FlowLauncher.Linux/FlowLauncher.Linux.csproj" ]; then
    VERSION="$(grep -oPm1 '(?<=<Version>)[^<]+' "$ROOT_DIR/src/FlowLauncher.Linux/FlowLauncher.Linux.csproj" || echo "1.0.0")"
else
    VERSION="1.0.0"
fi

# Sanitize version string for Debian package format (e.g. 1.0.0-beta.1 -> 1.0.0~beta.1)
VERSION="$(echo "$VERSION" | sed 's/^v//' | tr '-' '~')"
echo "Building Flow Launcher Debian package version: $VERSION"

# 2. Build .NET 8 application and plugins
echo "Publishing Flow Launcher .NET 8 application..."
mkdir -p "$PUBLISH_DIR"

if [ -f "$ROOT_DIR/src/FlowLauncher.Linux/FlowLauncher.Linux.csproj" ]; then
    dotnet publish "$ROOT_DIR/src/FlowLauncher.Linux/FlowLauncher.Linux.csproj" \
        -r linux-x64 \
        --self-contained false \
        -c Release \
        -o "$PUBLISH_DIR" \
        /p:Version="$VERSION"
else
    echo "Warning: FlowLauncher.Linux.csproj not found at expected path. Using existing publish directory if present."
fi

# Build and copy plugin DLLs
PLUGINS=("Plugin.Calculator" "Plugin.Shell" "Plugin.WebSearch" "Plugin.Explorer" "Plugin.Sys" "Plugin.ProcessKiller")
for plugin in "${PLUGINS[@]}"; do
    PLUGIN_CSPROJ="$ROOT_DIR/src/Plugins/$plugin/$plugin.csproj"
    if [ -f "$PLUGIN_CSPROJ" ]; then
        echo "Building plugin: $plugin"
        dotnet build "$PLUGIN_CSPROJ" \
            -c Release \
            -o "$PUBLISH_DIR/Plugins/$plugin"
    fi
done

# 3. Create Debian package directory structure
PKG_DIR="$OUTPUT_DIR/deb/flow-launcher_${VERSION}_amd64"
rm -rf "$PKG_DIR"
mkdir -p "$PKG_DIR/DEBIAN"
mkdir -p "$PKG_DIR/usr/bin"
mkdir -p "$PKG_DIR/usr/lib/flow-launcher"
mkdir -p "$PKG_DIR/usr/share/applications"
mkdir -p "$PKG_DIR/usr/share/icons/hicolor/256x256/apps"

# 4. Copy published binaries and plugins
echo "Staging files into package structure..."
cp -r "$PUBLISH_DIR"/* "$PKG_DIR/usr/lib/flow-launcher/"
chmod +x "$PKG_DIR/usr/lib/flow-launcher/FlowLauncher.Linux" 2>/dev/null || true
chmod +x "$PKG_DIR/usr/lib/flow-launcher/flow-launcher" 2>/dev/null || true

# Create executable wrapper in /usr/bin/flow-launcher
cat > "$PKG_DIR/usr/bin/flow-launcher" << 'EOF'
#!/bin/bash
if [ -x /usr/lib/flow-launcher/FlowLauncher.Linux ]; then
    exec /usr/lib/flow-launcher/FlowLauncher.Linux "$@"
elif [ -x /usr/lib/flow-launcher/flow-launcher ]; then
    exec /usr/lib/flow-launcher/flow-launcher "$@"
else
    exec dotnet /usr/lib/flow-launcher/FlowLauncher.Linux.dll "$@"
fi
EOF
chmod 755 "$PKG_DIR/usr/bin/flow-launcher"

# 5. Copy desktop file and icons
if [ -f "$SCRIPT_DIR/flow-launcher.desktop" ]; then
    cp "$SCRIPT_DIR/flow-launcher.desktop" "$PKG_DIR/usr/share/applications/"
    chmod 644 "$PKG_DIR/usr/share/applications/flow-launcher.desktop"
fi

if [ -f "$SCRIPT_DIR/flow-launcher.png" ]; then
    cp "$SCRIPT_DIR/flow-launcher.png" "$PKG_DIR/usr/share/icons/hicolor/256x256/apps/flow-launcher.png"
elif [ -f "$ROOT_DIR/assets/flow-launcher.png" ]; then
    cp "$ROOT_DIR/assets/flow-launcher.png" "$PKG_DIR/usr/share/icons/hicolor/256x256/apps/flow-launcher.png"
fi

# 6. Generate DEBIAN/control file
if [ -f "$SCRIPT_DIR/DEBIAN/control" ]; then
    sed "s/VERSION_PLACEHOLDER/$VERSION/g" "$SCRIPT_DIR/DEBIAN/control" > "$PKG_DIR/DEBIAN/control"
else
    cat > "$PKG_DIR/DEBIAN/control" << EOF
Package: flow-launcher
Version: $VERSION
Architecture: amd64
Maintainer: Flow Launcher Team <team@flowlauncher.com>
Depends: dotnet-runtime-8.0, libx11-6, xdg-utils, libnotify-bin
Recommends: locate | mlocate
Section: utils
Priority: optional
Description: Fast application launcher for Linux
 Fast, cross-platform application launcher inspired by Alfred and Wox.
 Supports plugins, web search, calculator, file search and more.
 Originally developed for Windows, now available on Linux.
EOF
fi
chmod 644 "$PKG_DIR/DEBIAN/control"

# 7. Create DEBIAN/postinst and postrm scripts
if [ -f "$SCRIPT_DIR/DEBIAN/postinst" ]; then
    cp "$SCRIPT_DIR/DEBIAN/postinst" "$PKG_DIR/DEBIAN/postinst"
else
    cat > "$PKG_DIR/DEBIAN/postinst" << 'EOF'
#!/bin/bash
set -e
update-desktop-database /usr/share/applications/ 2>/dev/null || true
echo "Flow Launcher installed. Run 'flow-launcher' to start."
exit 0
EOF
fi
chmod 755 "$PKG_DIR/DEBIAN/postinst"

if [ -f "$SCRIPT_DIR/DEBIAN/postrm" ]; then
    cp "$SCRIPT_DIR/DEBIAN/postrm" "$PKG_DIR/DEBIAN/postrm"
else
    cat > "$PKG_DIR/DEBIAN/postrm" << 'EOF'
#!/bin/bash
set -e
update-desktop-database /usr/share/applications/ 2>/dev/null || true
exit 0
EOF
fi
chmod 755 "$PKG_DIR/DEBIAN/postrm"

# 8. Build Debian package using dpkg-deb
DEB_FILE="$OUTPUT_DIR/flow-launcher_${VERSION}_amd64.deb"
echo "Building .deb package: $DEB_FILE"
dpkg-deb --build --root-owner-group "$PKG_DIR" "$DEB_FILE"

echo "Debian package successfully built: $DEB_FILE"
