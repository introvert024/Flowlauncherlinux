#!/bin/bash
# Install Flow Launcher to user's home directory
set -e
INSTALL_DIR="$HOME/.local/lib/flow-launcher"
BIN_DIR="$HOME/.local/bin"
SHARE_DIR="$HOME/.local/share"

mkdir -p "$INSTALL_DIR" "$BIN_DIR" "$SHARE_DIR/applications" "$SHARE_DIR/icons/hicolor/256x256/apps"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PUBLISH_DIR="$(dirname "$SCRIPT_DIR")/output/publish/linux-x64"

if [ ! -d "$PUBLISH_DIR" ]; then
    echo "Run build.sh first!"
    exit 1
fi

cp -r "$PUBLISH_DIR"/* "$INSTALL_DIR/"
chmod +x "$INSTALL_DIR/flow-launcher" 2>/dev/null || true
chmod +x "$INSTALL_DIR/FlowLauncher.Linux" 2>/dev/null || true

# Create wrapper script
cat > "$BIN_DIR/flow-launcher" << 'EOF'
#!/bin/bash
if [ -x "$HOME/.local/lib/flow-launcher/flow-launcher" ]; then
    exec "$HOME/.local/lib/flow-launcher/flow-launcher" "$@"
elif [ -x "$HOME/.local/lib/flow-launcher/FlowLauncher.Linux" ]; then
    exec "$HOME/.local/lib/flow-launcher/FlowLauncher.Linux" "$@"
else
    exec dotnet "$HOME/.local/lib/flow-launcher/FlowLauncher.Linux.dll" "$@"
fi
EOF
chmod +x "$BIN_DIR/flow-launcher"

# Desktop entry
cat > "$SHARE_DIR/applications/flow-launcher.desktop" << EOF
[Desktop Entry]
Version=1.0
Type=Application
Name=Flow Launcher
Comment=Fast application launcher
Exec=$BIN_DIR/flow-launcher
Icon=flow-launcher
Categories=Utility;System;
Keywords=launcher;search;
StartupNotify=false
EOF

update-desktop-database "$SHARE_DIR/applications" 2>/dev/null || true
echo "Flow Launcher installed! Add $BIN_DIR to your PATH if needed."
echo "Run: flow-launcher"
