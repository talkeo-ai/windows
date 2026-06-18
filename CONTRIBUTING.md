# Contributing to Talkeo for Windows

Thanks for your interest. Read this before opening a PR.

## Mindset

- **Use whatever stack/tool/AI you need.** No artificial limits on complexity or AI use. If a technology solves the problem cleanly, use it.
- **Vibecoding is welcome.** Use Claude Code, Cursor, GPT, whatever. What matters is the result and your understanding of it, not how you typed it.
- **Rigorous review.** Every PR is reviewed for four things:
  1. Does it solve a problem on an open issue or the roadmap?
  2. Does the author understand what they did and why?
  3. Is the code clean and consistent with the surrounding architecture?
  4. Was it actually built, run, and verified — not just generated?

If you can't answer all four, the PR will be closed (with feedback, no drama).

### Proof of work (required for review)

AI-generated code is welcome, but **AI output alone is never enough** — you must build it, run it, and confirm it works *before* requesting review. A PR that compiles in theory but was never run will be closed. Every PR must include:

- **A GIF or short screen recording of the change working.** Mandatory for any UI/UX change — it's the fastest proof the behavior is real. Drag the file into the PR description (GitHub hosts it; don't commit it to the repo).
- **Confirmation it builds** — `dotnet build src/Talkeo.Windows.csproj` clean.
- **What you actually tested** — the real manual steps you ran to verify it (which apps you selected text in, which DPI, etc.), not aspirational checkboxes.

## How to contribute

1. Find an issue labeled `good first issue` or `help wanted`, or pick a roadmap item.
2. Comment on the issue saying you want to work on it (avoids duplicate work).
3. Fork the repo, make your changes, open a PR. Reference the issue: `Closes #N`.
4. The maintainer reviews against the four criteria above.
5. Iterate or merge.

For tiny fixes (typos, obvious bugs), a direct PR without an issue is fine.

## Stack

C# / .NET 8. A Windows App SDK host with WPF windows for the tooltip and a WinForms tray, plus FlaUI for UI Automation — the combination needed to match the Mac floating-tooltip UX. The global mouse hook uses `SetWindowsHookEx` via P/Invoke. Build with `dotnet build src/Talkeo.Windows.csproj`.

## Conventions

- **English everywhere.** Code, comments, commits, PRs, issues.
- **Call the backend, never providers directly.** API keys live in the [Talkeo backend](https://github.com/talkeo-ai/talkeo), not in the client. The Windows app talks to `http://localhost:8000` (or the deployed backend), which talks to the providers.
- **UX parity with Mac.** The [Mac app](https://github.com/talkeo-ai/mac) is the reference for behavior. Mirror its decisions unless there's a platform reason to diverge.

## Maintainer

[@realjoaquinalvarez](https://github.com/realjoaquinalvarez) has final say on scope, architecture, and merges.

## License

By contributing, you agree your contributions are licensed under MIT (see [LICENSE](./LICENSE)).
