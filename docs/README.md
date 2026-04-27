# WCX Development Docs

This directory is the working memory for the long-running WCX modernization
project. Read this file first when resuming work.

## Current Project Direction

WCX is a modern successor to WC, not a direct UI port. The project should
preserve WC's MapleStory resource-format knowledge while introducing a clean,
testable, cross-platform architecture.

The first durable milestone is a headless core plus CLI:

1. Open a small WZ-like fixture.
2. Enumerate nodes.
3. Inspect node metadata.
4. Export at least one supported data type.
5. Run on macOS with tests.

Only after this should the Avalonia UI become feature-heavy.

## Document Map

- `architecture.md`: Layering, module boundaries, and dependency direction.
- `development-guidelines.md`: Coding, testing, git, and documentation rules.
- `migration-from-wc.md`: How to reuse WC without inheriting its coupling.
- `format-notes.md`: Working notes for WZ/MS/PKG behavior and fixture planning.
- `roadmap.md`: Milestones and next work queue.
- `commands.md`: Local development commands and known environment quirks.
- `decision-log.md`: Short index of architectural decisions.
- `adr/`: Longer decision records.
- `logs/`: Iteration notes for future context recovery.

## Naming

- WC: WzComparerR2, the existing maintained project.
- WCX: WzComparerX, this modernization project.

## Resume Checklist

When resuming work:

1. Check `git status --short --branch`.
2. Read the latest file in `docs/logs/`.
3. Review `docs/roadmap.md`.
4. Run `dotnet build WzComparerX.slnx --no-restore -m:1 -p:UseSharedCompilation=false`.
5. Run `dotnet test WzComparerX.slnx --no-build -m:1`.

If build or test commands fail in the Codex sandbox with MSBuild named-pipe
permission errors, rerun them with elevated permissions.
