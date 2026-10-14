# Troubleshooting & Linux Specifics

### 1. Global Hotkey on Wayland
**Symptom**: Pressing `Alt+Space` does not open the launcher on GNOME Wayland or KDE Wayland.

**Cause**: Wayland compositors (such as Mutter/GNOME and KWin) restrict client applications from snooping global keyboard events for security reasons (`XGrabKey` is an X11 protocol).

**Resolution**:
Register a custom shortcut in your desktop environment's settings:
- **GNOME**: Go to *Settings* → *Keyboard* → *Keyboard Shortcuts* → *Custom Shortcuts*. Add a shortcut named `Flow Launcher`, set the command to `flow-launcher --show`, and bind it to `Alt+Space`.
- **KDE Plasma**: Go to *System Settings* → *Shortcuts* → *Custom Shortcuts*. Add a new command shortcut executing `flow-launcher --show`.
- **Hyprland / Sway**: Add to `hyprland.conf`:
  ```conf
  bind = ALT, SPACE, exec, flow-launcher --show
  ```

---

### 2. Missing Icons in Search Results
**Symptom**: Search results display placeholder or blank icons.

**Cause**: Standard icon theme directories might not be indexed.

**Resolution**:
Ensure that the FreeDesktop icon themes are installed:
```bash
# Ubuntu / Debian
sudo apt install hicolor-icon-theme adwaita-icon-theme

# Fedora
sudo dnf install hicolor-icon-theme adwaita-icon-theme
```

---

### 3. File Search Not Returning Results
**Symptom**: File search with `/` or `~` is slow or does not find files across the filesystem.

**Cause**: `locate` / `plocate` database has not been initialized.

**Resolution**:
Install and update the fast locate database:
```bash
# Ubuntu / Debian
sudo apt install plocate
sudo updatedb

# Fedora
sudo dnf install plocate
sudo updatedb
```

---

### 4. Background Acrylic / Blur Effect Not Showing
**Symptom**: The launcher has an opaque or flat background instead of acrylic blur.

**Cause**: The running desktop compositor does not support window-level background blur, or blur is disabled in your window manager.

**Resolution**:
- On KDE: Ensure *Blur* desktop effect is enabled in *System Settings* → *Workspace Behavior* → *Desktop Effects*.
- On X11 Window Managers (i3, bspwm, XFCE): Install and run `picom` with experimental dual-kawase blur:
  ```bash
  picom --experimental-backends --backend glx
  ```
