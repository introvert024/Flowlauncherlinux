#!/bin/bash
# Build script for Flow Launcher Linux
set -e
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
VERSION=${FLOW_VERSION:-"1.0.0"}
OUTPUT_DIR="$ROOT_DIR/output"
PUBLISH_DIR="$OUTPUT_DIR/publish"

echo "Building Flow Launcher $VERSION for Linux"
mkdir -p "$PUBLISH_DIR"

# Restore
dotnet restore "$ROOT_DIR/FlowLauncher.Linux.sln"

# Build main app
dotnet publish "$ROOT_DIR/src/FlowLauncher.Linux/FlowLauncher.Linux.csproj" \
    -r linux-x64 \
    --self-contained false \
    -c Release \
    -o "$PUBLISH_DIR/linux-x64" \
    /p:Version="$VERSION"

# Build arm64
dotnet publish "$ROOT_DIR/src/FlowLauncher.Linux/FlowLauncher.Linux.csproj" \
    -r linux-arm64 \
    --self-contained false \
    -c Release \
    -o "$PUBLISH_DIR/linux-arm64" \
    /p:Version="$VERSION"

# Build and copy plugins
PLUGINS=("Plugin.Applications" "Plugin.Calculator" "Plugin.Shell" "Plugin.WebSearch" "Plugin.Explorer" "Plugin.Sys" "Plugin.ProcessKiller")
for plugin in "${PLUGINS[@]}"; do
    echo "Building plugin: $plugin"
    dotnet build "$ROOT_DIR/src/Plugins/$plugin/$plugin.csproj" \
        -c Release \
        -o "$PUBLISH_DIR/linux-x64/Plugins/$plugin"
done

echo "Build complete! Output in $PUBLISH_DIR"
