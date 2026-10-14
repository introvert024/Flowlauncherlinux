# Flow Launcher for Linux

> A native Linux port of [Flow Launcher](https://github.com/Flow-Launcher/Flow.Launcher) — the fast, productive, open-source application launcher for Linux (.NET 8 + Avalonia UI).

[![Build Linux Packages](https://github.com/introvert024/Flowlauncherlinux/actions/workflows/build-linux.yml/badge.svg)](https://github.com/introvert024/Flowlauncherlinux/actions/workflows/build-linux.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

## Features

- 🚀 **Fast fuzzy search** across applications, files, and web
- 🧩 **Plugin system** — same JSON-RPC protocol as original Flow Launcher (Python, Node.js, C# plugins)
- ⌨️ **Global hotkey** (`Alt+Space` by default) to summon the launcher
- 🎨 **Acrylic/blur UI** via Avalonia on supported compositors
- 📦 **Built-in plugins**:
  - **Applications**: Search and launch installed `.desktop` apps
  - **Calculator**: Evaluate math expressions via `mXparser`
  - **Explorer**: File/folder search using fast `locate` and path completion
  - **Shell**: Run terminal commands with auto-detected terminal emulators
  - **WebSearch**: Instant queries across Google, DuckDuckGo, Bing, GitHub, YouTube
  - **Sys**: System actions (shutdown, restart, lock, sleep, logout, trash)
  - **ProcessKiller**: Find and kill running processes (`kill <name>`)
- 🐧 **Works on Ubuntu, Debian, Fedora, Arch, openSUSE** and modern Linux distributions
- 🔧 **X11 + Wayland** support
- 🏗️ **Ready-to-use packages**: `.deb`, `.rpm`, and standalone `AppImage`

---

## Documentation

- 📐 [Architecture & Design Details](docs/ARCHITECTURE.md)
- 📦 [Packaging Guide (.deb, .rpm, AppImage)](docs/PACKAGING.md)
- 🔌 [Plugin Development Guide](docs/PLUGIN_DEVELOPMENT.md)
- ⚙️ [Configuration Guide](docs/CONFIGURATION.md)
- 🛠️ [Troubleshooting (Wayland, Icons, Blur)](docs/TROUBLESHOOTING.md)

---

## Installation

### Ubuntu / Debian (.deb)

```bash
# Download latest .deb from Releases
wget https://github.com/introvert024/Flowlauncherlinux/releases/latest/download/flow-launcher_amd64.deb
sudo dpkg -i flow-launcher_amd64.deb
sudo apt-get install -f   # Fix dependencies if needed
```

### Fedora / RHEL / openSUSE (.rpm)

```bash
wget https://github.com/introvert024/Flowlauncherlinux/releases/latest/download/flow-launcher.x86_64.rpm
sudo dnf install flow-launcher.x86_64.rpm
```

### AppImage (Universal)

```bash
wget https://github.com/introvert024/Flowlauncherlinux/releases/latest/download/FlowLauncher-x86_64.AppImage
chmod +x FlowLauncher-x86_64.AppImage
./FlowLauncher-x86_64.AppImage
```

### Build & Install from Source

```bash
git clone https://github.com/introvert024/Flowlauncherlinux.git
cd Flowlauncherlinux
bash scripts/build.sh       # Requires .NET 8 SDK
bash scripts/install.sh     # Installs to ~/.local/
```

---

## Prerequisites

| Dependency | Required | Purpose |
|---|---|---|
| `.NET 8 Runtime` | ✅ Yes | Application runtime |
| `libx11-6` | ✅ Yes (X11) | Global hotkey on X11 |
| `xdg-utils` | ✅ Yes | Open URLs, files, folders |
| `libnotify-bin` | Recommended | Desktop notifications |
| `locate` / `mlocate` | Optional | Fast file search |
| `xclip` or `xsel` | Optional | Clipboard support |
| `python3` | Optional | Python plugins |
| `nodejs` | Optional | Node.js plugins |

### Install .NET 8 Runtime

```bash
# Ubuntu/Debian
sudo apt install dotnet-runtime-8.0

# Fedora
sudo dnf install dotnet-runtime-8.0

# Arch
sudo pacman -S dotnet-runtime-8
```

---

## Usage

| Action | Default |
|---|---|
| Open launcher | `Alt + Space` |
| Close launcher | `Esc` |
| Navigate results | `↑` / `↓` |
| Open result | `Enter` |
| Run as admin/elevated | `Shift + Enter` |
| Auto-complete | `Tab` |

### Built-in Plugins

| Plugin | Keyword | Example |
|---|---|---|
| Web Search | `g` | `g how to linux` → opens Google |
| Calculator | (global) | `2 + 2 * 3` → `8` |
| Terminal | `>` | `> ls -la` → runs command |
| File Search | `/` or `~` | `/home/user/Documents` |
| System Commands | (global) | `shutdown`, `restart`, `lock` |
| Process Killer | `kill` | `kill chrome` |

---

## Configuration

Settings are stored at `~/.config/flow-launcher/settings.json`:

```json
{
  "Hotkey": "alt+space",
  "StartupAutoRun": false,
  "HideOnLostFocus": true,
  "MaxResultsToShow": 6,
  "Theme": "Light",
  "MainWindowWidth": 580,
  "Language": "en-us"
}
```

---

## Architecture

The Linux port replaces Windows-specific technologies with cross-platform equivalents:

| Windows (Original) | Linux Port |
|---|---|
| WPF (XAML UI) | **Avalonia UI** (AXAML) |
| Win32 API / PInvoke | Linux syscalls / X11 / D-Bus |
| Windows Registry | `~/.config/flow-launcher/` JSON |
| `NHotkey.Wpf` | X11 `XGrabKey` / atspi |
| Task Scheduler | `~/.config/autostart/` |
| `squirrel.windows` | AppImage / apt / dnf |
| Windows notifications | `notify-send` |
| Shell executor | `bash -c` / `xterm` |

### Project Structure

```
linux-port/
├── src/
│   ├── FlowLauncher.Linux/          # Main Avalonia UI app
│   │   ├── Views/MainWindow.axaml   # Main search window
│   │   ├── ViewModels/              # MVVM ViewModels
│   │   └── Program.cs               # Entry point
│   ├── FlowLauncher.Linux.Core/     # Plugin management, JsonRPC
│   ├── FlowLauncher.Linux.Infrastructure/  # Cross-platform helpers
│   │   ├── Hotkey/                  # X11 global hotkey
│   │   ├── XdgApplications/         # .desktop file scanner
│   │   └── Storage/                 # JSON settings storage
│   ├── FlowLauncher.Linux.Plugin/   # Plugin SDK (IPlugin, Result, Query)
│   └── Plugins/
│       ├── Plugin.Calculator/
│       ├── Plugin.Shell/
│       ├── Plugin.WebSearch/
│       ├── Plugin.Explorer/
│       ├── Plugin.Sys/
│       └── Plugin.ProcessKiller/
├── packaging/
│   ├── build-deb.sh                 # Build .deb package
│   ├── build-rpm.sh                 # Build .rpm package
│   ├── build-appimage.sh            # Build AppImage
│   └── flow-launcher.desktop        # XDG desktop entry
├── scripts/
│   ├── build.sh                     # Build all
│   ├── install.sh                   # Install to ~/.local/
│   └── uninstall.sh
└── .github/workflows/
    └── build-linux.yml              # CI/CD pipeline
```

---

## Building from Source

### Requirements
- .NET 8 SDK
- `dpkg-dev` (for .deb)
- `rpm` (for .rpm)

```bash
cd linux-port

# Build everything
bash scripts/build.sh

# Create .deb
bash packaging/build-deb.sh

# Create .rpm
bash packaging/build-rpm.sh

# Create AppImage
bash packaging/build-appimage.sh
```

---

## Plugin Compatibility

The plugin protocol is **fully compatible** with Flow Launcher's existing plugin ecosystem. Python and Node.js plugins work without modification. C# plugins need to be recompiled targeting `net8.0` without WPF dependencies.

---

## Wayland Support

On Wayland, global hotkeys require `atspi` or the compositor's keybinding API. The launcher starts correctly on Wayland but the global hotkey may require:

```bash
# GNOME Wayland: Set a custom keyboard shortcut in Settings → Keyboard → Custom Shortcuts
# Point it to: flow-launcher --show

# KDE Wayland: Use KDE's custom shortcuts
```

---

## Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-feature`
3. Build: `bash linux-port/scripts/build.sh`
4. Submit a PR

---

## License

MIT — same as Flow Launcher. See [LICENSE](../LICENSE).

---

## Credits

Built on the foundation of [Flow Launcher](https://github.com/Flow-Launcher/Flow.Launcher) by the Flow Launcher Team.
UI powered by [Avalonia UI](https://avaloniaui.net/).
