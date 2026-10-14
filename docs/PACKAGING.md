# Packaging & Distribution Guide for Linux

Flow Launcher Linux supports three primary distribution channels:
1. **Debian / Ubuntu Package (`.deb`)**
2. **Fedora / Red Hat Package (`.rpm`)**
3. **Universal AppImage (`.AppImage`)**

---

## 1. Debian / Ubuntu Package (`.deb`)

### Build Command
```bash
cd linux-port
FLOW_VERSION=1.0.0 bash packaging/build-deb.sh
```

### Generated Layout
```
flow-launcher_1.0.0_amd64/
├── DEBIAN/
│   ├── control       # Package metadata and runtime dependencies
│   ├── postinst      # Post-install hook: updates desktop database
│   └── postrm        # Post-remove hook
└── usr/
    ├── bin/
    │   └── flow-launcher                    # Symlink / launch wrapper
    ├── lib/flow-launcher/                   # Compiled .NET 8 binaries and built-in plugins
    │   ├── flow-launcher
    │   └── Plugins/
    ├── share/applications/
    │   └── flow-launcher.desktop           # System launcher entry
    └── share/icons/hicolor/256x256/apps/
        └── flow-launcher.png               # Application icon
```

### Installation
```bash
sudo dpkg -i output/flow-launcher_1.0.0_amd64.deb
sudo apt-get install -f
```

---

## 2. Fedora / Red Hat Package (`.rpm`)

### Prerequisites
```bash
sudo dnf install -y rpm-build
```

### Build Command
```bash
cd linux-port
FLOW_VERSION=1.0.0 bash packaging/build-rpm.sh
```

### Package Metadata
Managed by `packaging/flow-launcher.spec`, defining:
- `Requires: dotnet-runtime-8.0, libX11, xdg-utils, libnotify`
- Post-install invocation of `/usr/bin/update-desktop-database`

### Installation
```bash
sudo dnf install output/flow-launcher-1.0.0-1.x86_64.rpm
```

---

## 3. Universal AppImage

The AppImage package bundles the launcher into an executable container runnable on almost all modern Linux distributions without requiring root privileges.

### Prerequisites
```bash
sudo apt install -y libfuse2 # or fuse on Fedora/Arch
```

### Build Command
```bash
cd linux-port
FLOW_VERSION=1.0.0 bash packaging/build-appimage.sh
```

### Running AppImage
```bash
chmod +x output/FlowLauncher-1.0.0-x86_64.AppImage
./output/FlowLauncher-1.0.0-x86_64.AppImage
```

---

## 4. Automated GitHub Actions CI/CD

Whenever a new version tag (`v1.0.0`) is pushed to the repository:
- `.github/workflows/build-linux.yml` automatically triggers on Ubuntu 22.04 runners.
- Builds binary packages for `linux-x64` and `linux-arm64`.
- Packages `.deb`, `.rpm`, and `.AppImage`.
- Automatically drafts and publishes a GitHub Release with attached downloadable assets.
