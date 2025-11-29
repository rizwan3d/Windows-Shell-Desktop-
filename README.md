# Shell Desktop (WPF)

A minimal custom **Windows desktop shell** written in WPF. It mirrors your real desktop icons, shows your current wallpaper across all monitors, and renders its **own taskbar** with running windows and a clock while hiding the original Explorer taskbar.

> ⚠️ This is an experimental shell-style app for Windows. Use with care, and don’t set it as your system shell unless you know how to recover from a broken configuration.

---

## Features

### 🖥 Desktop icon mirroring

- Reads the **actual desktop icons and their on-screen positions** directly from Explorer’s `SysListView32` (`FolderView`) using `LVM_GETITEMCOUNT`, `LVM_GETITEMTEXTW`, and `LVM_GETITEMPOSITION` over remote process memory.   
- Each icon is rendered into a WPF `Canvas` as a `StackPanel` with an image and text label.   
- If reading from the shell fails, it falls back to a **simple grid layout** that enumerates files and folders from the user and common desktop directories.   

### 📂 Real file & special folder icons

- Uses `SHGetFileInfo` with `SHGFI.Icon` to obtain proper **file/folder icons** as `ImageSource` objects via `Imaging.CreateBitmapSourceFromHIcon`.   
- Supports **special shell locations** such as:
  - This PC / My Computer  
  - Network  
  - Recycle Bin  
  - Control Panel   
- Special items resolve to `shell:` URIs like `shell:MyComputerFolder`, `shell:RecycleBinFolder`, etc., so they open correctly in Explorer.   

### 🧠 Icon layout persistence

- Icon positions are saved in a JSON file:
  - `%LOCALAPPDATA%\ShellDesktop\layout.json`   
- When the app starts, it reloads this layout and repositions icons accordingly.  
- Supports:
  - Click-to-select  
  - Ctrl+Click multi-select  
  - Dragging one or multiple icons  
  - Rubber-band selection rectangle over the desktop canvas   

### 🖱 Context menu per icon

Right-clicking a desktop icon shows a context menu with:   

- **Open** – launches the file/folder or special shell location (via `ProcessStartInfo`).  
- **Delete** – confirms and deletes files/folders, then refreshes the desktop.  
- **Show in Explorer** – opens File Explorer pointing at the selected item.  

### 🌄 Multi-monitor wallpaper

- Reads the current desktop wallpaper path via `SystemParametersInfo(SPI_GETDESKWALLPAPER)`.   
- For each monitor (via `EnumDisplayMonitors`), it draws an `Image` onto a `WallpaperCanvas`, stretching the wallpaper appropriately across the virtual screen.   

### 🧰 Custom taskbar (per monitor)

- Enumerates display monitors and creates a **floating, rounded taskbar** on each, near the bottom center.   
- Each taskbar instance contains:
  - A **Start button** that launches `explorer.exe`.   
  - A **window strip** of buttons, one per visible top-level window.  
  - A **digital clock** (updated every second) showing `HH:mm`.   
- Top-level windows are discovered using `EnumWindows`, `GetAncestor(GA_ROOTOWNER)`, `IsWindowVisible`, and `GetWindowText`.   
- Window buttons:
  - Show the app’s executable icon (via `GetWindowThreadProcessId` → process path → `ShellIcon.GetFileIconSource`).   
  - On click: restore if minimized and bring the window to the foreground with `ShowWindow(SW_RESTORE)` + `SetForegroundWindow`.   

### 🕶 Hiding / restoring the system taskbar

- Finds primary & secondary taskbar windows (`Shell_TrayWnd` and `Shell_SecondaryTrayWnd`) and hides them using `ShowWindow(SW_HIDE)` when the shell loads.   
- On exit, it restores them with `ShowWindow(SW_SHOW)` to bring back the original Explorer taskbar.   
