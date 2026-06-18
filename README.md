# Talkeo — Windows

Native Windows implementation of Talkeo. Detects text selection system-wide and shows a floating action tooltip near the cursor.

## Prerequisites

- Windows 10 19041+ or Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) — verify with `dotnet --version`

## Build

```powershell
dotnet build src/Talkeo.Windows.csproj
```

## Run

```powershell
dotnet run --project src/Talkeo.Windows.csproj
```

A Talkeo icon appears in the system tray. Select text in any app and release the mouse — the tooltip appears near the cursor.

## Architecture

```
src/
├── App.xaml(.cs)              # Entry point + orchestration
├── NativeMethods.cs           # Shared Win32 P/Invoke helpers
├── Hooks/
│   └── MouseHook.cs           # Global WH_MOUSE_LL hook (drag + double-click detection)
├── Selection/
│   └── SelectionReader.cs     # UI Automation primary path + Ctrl+C clipboard fallback
├── UI/
│   ├── TooltipWindow.xaml     # Floating window layout (chromeless, frosted glass)
│   └── TooltipWindow.xaml.cs  # Window styling + show/position logic
└── Tray/
    └── TrayIcon.cs            # System tray icon with quit menu
```

## Flow

```
WH_MOUSE_LL WM_LBUTTONUP (drag or double-click)
    → 80ms delay
    → SelectionReader: IUIAutomationTextPattern.GetSelection()
        → fallback: snapshot clipboard → SendInput(Ctrl+C) → read → restore
    → DispatcherQueue.TryEnqueue → TooltipWindow.Show(text, cursor)
```

## Stack

| Technology | Purpose |
|---|---|
| Windows App SDK 1.6 | App host |
| WPF (chromeless `Window`) | Floating tooltip — transparent, avoids the Win11 compositor border |
| FlaUI.UIA3 | UI Automation wrapper for reading selected text |
| System.Windows.Forms.NotifyIcon | System tray icon |
| SetWindowsHookEx (P/Invoke) | Global low-level mouse hook |

The stack is a hybrid (Windows App SDK host + WPF windows + WinForms tray + FlaUI) — the combination needed to match the Mac floating-tooltip UX.

## Known Limitations

- Tooltip does not auto-hide on outside click (close button only) — planned for a future issue
- Acrylic backdrop requires Windows 10 19041+
- Rounded corners require Windows 11 (silently no-op on Windows 10)
- Electron-based apps (Slack, Discord, VS Code) may not expose UI Automation TextPattern; clipboard fallback handles these

## Out of Scope (follow-up issues)

- LLM / TTS provider integration
- Settings persistence
- Auto-hide on outside click
- Replace-in-place via UI Automation write

## Shared with macOS

**No code is shared across platforms.** Each platform implements the same UX natively. Only API contracts and design tokens in `shared/` are common (planned).
