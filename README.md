# Talkeo for Windows

Native Windows 11 implementation of Talkeo. **Not started yet.**

## Planned stack

- **WinUI 3** (modern native Windows UI framework)
- **C# / .NET 8+**
- **UI Automation API** for text selection (Windows equivalent of macOS Accessibility API)
- **`SetWindowsHookEx`** via P/Invoke for global keyboard/mouse hooks
- **NotifyIcon** for system tray

## Why this stack

- Native (matches the macOS Swift + AppKit approach in spirit).
- Modern, actively maintained by Microsoft (unlike WPF or Win32).
- Friendly enough not to shoot ourselves in the foot (unlike raw Win32 / C++).
- Good interop with the rest of the .NET ecosystem (HTTP, JSON, SQLite, etc.).

## Status

First milestone: skeleton WinUI 3 project + text selection detection via UI Automation. Pick up the issue if you want to contribute.

## License

MIT. See [LICENSE](./LICENSE).

## Talkeo ecosystem

See the organization page at [github.com/talkeo-ai](https://github.com/talkeo-ai) for the full product context.
