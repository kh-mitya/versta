@AGENTS.md

## Claude Code notes

- The owner writes in Russian, so reply in Russian.
- Preview with the `site` entry in `.claude/launch.json` (http://localhost:8765). The owner opens `index.html` straight from disk, so behaviour on `file://` (clipboard, Referer-less tile requests) matters too.
- When the owner asks for the file, send `index.html` itself.
- Don't push to `main` (it redeploys the live site) or publish anywhere else without an explicit go-ahead.
