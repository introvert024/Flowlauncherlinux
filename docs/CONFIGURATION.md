# Configuration Guide

Flow Launcher Linux adheres to the XDG Base Directory specification. All persistent configuration and user data are located under:
`~/.config/flow-launcher/`

---

## 1. Primary Settings File

Location: `~/.config/flow-launcher/settings.json`

```json
{
  "Hotkey": "alt+space",
  "StartupAutoRun": false,
  "HideOnLostFocus": true,
  "RememberLastLaunchLocation": false,
  "WindowLeft": -1,
  "WindowTop": -1,
  "MainWindowWidth": 580,
  "MaxResultsToShow": 6,
  "Theme": "Light",
  "ShowResultsIcons": true,
  "Language": "en-us",
  "ShowOpenResultHotkey": true,
  "AlwaysPreview": false,
  "QuerySearchPrecision": false,
  "AnimationSpeed": 300,
  "Plugins": [
    {
      "ID": "B3B21FCD-0001-4E02-A89F-C8FCB4D4E37E",
      "Disabled": false,
      "Priority": 0,
      "ActionKeyword": "*"
    },
    {
      "ID": "0EF1B6E2-F971-432F-9CC0-C09A9EC4C4D1",
      "Disabled": false,
      "Priority": 0,
      "ActionKeyword": ">"
    }
  ]
}
```

---

## 2. Hotkey Formats

Supported modifiers:
- `alt`
- `ctrl` / `control`
- `shift`
- `super` / `win`

Keys:
- `space`, `return`, `enter`, `escape`, `tab`, `f1`-`f12`, single characters (`a`-`z`, `0`-`9`)

Example combinations:
- `alt+space` (default)
- `ctrl+space`
- `super+space`
- `ctrl+alt+p`

---

## 3. Autostart Configuration

When `StartupAutoRun` is enabled, Flow Launcher creates an autostart desktop entry at:
`~/.config/autostart/flow-launcher.desktop`

```ini
[Desktop Entry]
Type=Application
Name=Flow Launcher
Exec=/usr/bin/flow-launcher --start-hidden
Hidden=false
NoDisplay=false
X-GNOME-Autostart-enabled=true
```
