# Talkeo for Windows

Native Windows implementation of Talkeo. The Windows companion to the [Mac app](https://github.com/talkeo-ai/mac), consuming the [Talkeo backend](https://github.com/talkeo-ai/talkeo).

## Stack

- **WPF** (Windows Presentation Foundation)
- **C# / .NET 8+**
- **UI Automation API** for text selection (Windows equivalent of macOS Accessibility API)
- **`SetWindowsHookEx`** via P/Invoke for global keyboard/mouse hooks
- **NotifyIcon** for system tray

## Why WPF

- Mature, production-proven framework with extensive ecosystem and documentation.
- Better control over windowing behavior than WinUI 3 for the floating-tooltip pattern Talkeo uses.
- Good interop with the rest of the .NET ecosystem (HTTP, JSON, SQLite, etc.).
- Native enough to match the macOS Swift + AppKit approach in spirit, without raw Win32 / C++ pain.

## Status

Active development. First milestone (skeleton WPF project + text selection detection via UI Automation) in progress. See open issues and PRs for the current state.

## License

MIT. See [LICENSE](./LICENSE).

## Talkeo ecosystem

See the [organization page](https://github.com/talkeo-ai) and the [ROADMAP](https://github.com/talkeo-ai/.github/blob/main/profile/ROADMAP.md) for the full product context.
