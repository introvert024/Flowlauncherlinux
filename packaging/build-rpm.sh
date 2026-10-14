#!/bin/bash
# Build RPM package for Flow Launcher Linux
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

# Sanitize version string for RPM package format (hyphens replaced by underscore or dot)
VERSION="$(echo "$VERSION" | sed 's/^v//' | tr '-' '.')"
echo "Building Flow Launcher RPM package version: $VERSION"

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

# Build plugins
PLUGINS=("Plugin.Applications" "Plugin.Calculator" "Plugin.Shell" "Plugin.WebSearch" "Plugin.Explorer" "Plugin.Sys" "Plugin.ProcessKiller")
for plugin in "${PLUGINS[@]}"; do
    PLUGIN_CSPROJ="$ROOT_DIR/src/Plugins/$plugin/$plugin.csproj"
    if [ -f "$PLUGIN_CSPROJ" ]; then
        echo "Building plugin: $plugin"
        dotnet build "$PLUGIN_CSPROJ" \
            -c Release \
            -o "$PUBLISH_DIR/Plugins/$plugin"
    fi
done

# 3. Create RPM spec directory structure
RPMBUILD_DIR="${RPMBUILD_ROOT:-$HOME/rpmbuild}"
echo "Setting up rpmbuild directory structure in: $RPMBUILD_DIR"
mkdir -p "$RPMBUILD_DIR"/{BUILD,RPMS,SOURCES,SPECS,SRPMS}

# 4. Create the archive
ARCHIVE_NAME="flow-launcher-$VERSION"
STAGE_DIR="$OUTPUT_DIR/$ARCHIVE_NAME"
rm -rf "$STAGE_DIR"
mkdir -p "$STAGE_DIR/out"

cp -r "$PUBLISH_DIR"/* "$STAGE_DIR/out/"
if [ -f "$SCRIPT_DIR/flow-launcher.desktop" ]; then
    cp "$SCRIPT_DIR/flow-launcher.desktop" "$STAGE_DIR/"
    cp "$SCRIPT_DIR/flow-launcher.desktop" "$RPMBUILD_DIR/SOURCES/"
fi

tar -czf "$RPMBUILD_DIR/SOURCES/$ARCHIVE_NAME.tar.gz" -C "$OUTPUT_DIR" "$ARCHIVE_NAME"
rm -rf "$STAGE_DIR"

# 5. Generate / copy spec file
SPEC_FILE="$RPMBUILD_DIR/SPECS/flow-launcher.spec"
if [ -f "$SCRIPT_DIR/flow-launcher.spec" ]; then
    cp "$SCRIPT_DIR/flow-launcher.spec" "$SPEC_FILE"
else
    cat > "$SPEC_FILE" << EOF
Name:           flow-launcher
Version:        $VERSION
Release:        1%{?dist}
Summary:        Fast application launcher for Linux

License:        MIT
URL:            https://github.com/Flow-Launcher/Flow.Launcher
Source0:        %{name}-%{version}.tar.gz
Source1:        flow-launcher.desktop

BuildArch:      x86_64
Requires:       dotnet-runtime-8.0
Requires:       libX11
Requires:       xdg-utils
Requires:       libnotify

%description
Fast, cross-platform application launcher inspired by Alfred and Wox.
Supports plugins, web search, calculator, file search and more.
Originally developed for Windows, now available on Linux using Avalonia UI.

%prep
%setup -q

%build
# Pre-built binaries are packaged

%install
rm -rf %{buildroot}
mkdir -p %{buildroot}%{_bindir}
mkdir -p %{buildroot}%{_prefix}/lib/%{name}
mkdir -p %{buildroot}%{_datadir}/applications

# Copy binaries
cp -r out/* %{buildroot}%{_prefix}/lib/%{name}/

# Create wrapper script
cat > %{buildroot}%{_bindir}/%{name} << 'WRAPPER'
#!/bin/bash
if [ -x %{_prefix}/lib/%{name}/FlowLauncher.Linux ]; then
    exec %{_prefix}/lib/%{name}/FlowLauncher.Linux "\$@"
elif [ -x %{_prefix}/lib/%{name}/flow-launcher ]; then
    exec %{_prefix}/lib/%{name}/flow-launcher "\$@"
else
    exec dotnet %{_prefix}/lib/%{name}/FlowLauncher.Linux.dll "\$@"
fi
WRAPPER
chmod 755 %{buildroot}%{_bindir}/%{name}

# Install desktop entry
if [ -f %{SOURCE1} ]; then
    install -m 644 %{SOURCE1} %{buildroot}%{_datadir}/applications/%{name}.desktop
elif [ -f %{name}.desktop ]; then
    install -m 644 %{name}.desktop %{buildroot}%{_datadir}/applications/%{name}.desktop
fi

%post
/sbin/ldconfig
/usr/bin/update-desktop-database %{_datadir}/applications &> /dev/null || :

%postun
/sbin/ldconfig
/usr/bin/update-desktop-database %{_datadir}/applications &> /dev/null || :

%files
%{_bindir}/%{name}
%{_prefix}/lib/%{name}
%{_datadir}/applications/%{name}.desktop

%changelog
* Wed Oct 14 2026 Flow Launcher Team <team@flowlauncher.com> - $VERSION-1
- Flow Launcher Linux release
EOF
fi

# 6. Run rpmbuild
echo "Running rpmbuild..."
rpmbuild -bb --define "version $VERSION" "$SPEC_FILE"

# 7. Copy final RPM out
mkdir -p "$OUTPUT_DIR"
find "$RPMBUILD_DIR/RPMS" -name "*.rpm" -exec cp {} "$OUTPUT_DIR/" \;

echo "RPM package(s) built and copied to $OUTPUT_DIR:"
ls -lh "$OUTPUT_DIR"/*.rpm 2>/dev/null || true
