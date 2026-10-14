Name:           flow-launcher
Version:        %{?version}%{!?version:1.0.0}
Release:        1%{?dist}
Summary:        Fast application launcher for Linux

License:        MIT
URL:            https://github.com/Flow-Launcher/Flow.Launcher
Source0:        %{name}-%{version}.tar.gz
Source1:        flow-launcher.desktop

BuildArch:      x86_64
BuildRequires:  dotnet-sdk-8.0
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
# If building from source csproj is present:
if [ -f FlowLauncher.Linux.csproj ]; then
    dotnet publish FlowLauncher.Linux.csproj -r linux-x64 --self-contained false -c Release -o out/
fi

%install
rm -rf %{buildroot}
mkdir -p %{buildroot}%{_bindir}
mkdir -p %{buildroot}%{_prefix}/lib/%{name}
mkdir -p %{buildroot}%{_datadir}/applications
mkdir -p %{buildroot}%{_datadir}/icons/hicolor/256x256/apps

# Copy binaries and assets to /usr/lib/flow-launcher/
if [ -d out ]; then
    cp -r out/* %{buildroot}%{_prefix}/lib/%{name}/
else
    cp -r * %{buildroot}%{_prefix}/lib/%{name}/
fi

# Ensure main binaries have executable permissions
chmod 755 %{buildroot}%{_prefix}/lib/%{name}/FlowLauncher.Linux 2>/dev/null || true
chmod 755 %{buildroot}%{_prefix}/lib/%{name}/flow-launcher 2>/dev/null || true

# Create wrapper script in /usr/bin/flow-launcher
cat > %{buildroot}%{_bindir}/%{name} << 'EOF'
#!/bin/bash
if [ -x %{_prefix}/lib/%{name}/FlowLauncher.Linux ]; then
    exec %{_prefix}/lib/%{name}/FlowLauncher.Linux "$@"
elif [ -x %{_prefix}/lib/%{name}/flow-launcher ]; then
    exec %{_prefix}/lib/%{name}/flow-launcher "$@"
else
    exec dotnet %{_prefix}/lib/%{name}/FlowLauncher.Linux.dll "$@"
fi
EOF
chmod 755 %{buildroot}%{_bindir}/%{name}

# Install desktop entry
if [ -f %{SOURCE1} ]; then
    install -m 644 %{SOURCE1} %{buildroot}%{_datadir}/applications/%{name}.desktop
elif [ -f %{name}.desktop ]; then
    install -m 644 %{name}.desktop %{buildroot}%{_datadir}/applications/%{name}.desktop
fi

# Install icon if present
if [ -f flow-launcher.png ]; then
    install -m 644 flow-launcher.png %{buildroot}%{_datadir}/icons/hicolor/256x256/apps/%{name}.png
fi

%post
/sbin/ldconfig
/usr/bin/update-desktop-database %{_datadir}/applications &> /dev/null || :

%postun
/sbin/ldconfig
/usr/bin/update-desktop-database %{_datadir}/applications &> /dev/null || :

%files
%{_bindir}/%{name}
%{_prefix}/lib/%{name}/
%{_datadir}/applications/%{name}.desktop

%changelog
* Wed Oct 14 2026 Flow Launcher Team <team@flowlauncher.com> - %{version}-1
- Flow Launcher Linux RPM package
