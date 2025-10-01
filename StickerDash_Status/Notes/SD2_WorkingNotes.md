# StickerDash2.0 — Working Notes
- Installer run at 20251001_093630
- Defaults: meters unless stated; curves/slopes assume degrees if omitted.
- Menus: Window → Aim2Pro → Workbench → SD2 Workbench; Window → Aim2Pro → Terminal → Open Project Root.
- Data packs installed: commands, macros, lexicon, fieldmap, registry, schema, normalizer, tests, UnknownQueue.## Update $(date +%Y-%m-%d' '%H:%M:%S)
- Installed original Workbench, AIGG windows, API Settings, Force Recompile, Restore Point tools, Track Generator (Option B).
- Ensured Resources/Spec/intents.json exists (meters-by-default).
- Removed any placeholder stubs to keep menus tidy.

## Update 2025-10-01 11:34:30
- Installed your original files (Option B FIX) and removed stubs.
- Ensured Resources/Spec/intents.json exists (meters-by-default).

## Update 2025-10-01 11:53:17
- Installed your original files (Option B FIX) and removed stubs.
- Ensured Resources/Spec/intents.json exists (meters-by-default).

## Update 2025-10-01 12:05:43
- Installed your original files (Option B EXACT) and removed stubs.
- Ensured Resources/Spec/intents.json exists (meters-by-default).

## Update 2025-10-01 12:12:38
- Installed minimal, compile-safe Editor windows (menus under Window → Aim2Pro).
- meters-by-default intents.json ensured.

## Update 2025-10-01 12:54:20
- Added 'Open Project Root' under Window → Aim2Pro → Terminal (hotkey: Cmd/Ctrl+T).

## Git Setup 2025-10-01 13:05:48
- Initialized Git, added Unity .gitignore, configured Git LFS.

## Update 2025-10-01 13:44:34
- Added Track Generator v2: NL → plan → preview/run (meters/deg default).
- Menu: Window → Aim2Pro → Track Creator → Track Generator v2

## 2025-10-01 14:01:09 — Specs Registry Installed
- Auto builds StickerDash_Status/Specs/SD2_Specs.json with hashes + timestamps of all key files.
- Menu: Window → Aim2Pro → Status → {Open Specs Register, Force Rescan Specs, Add Spec Note…}

## 2025-10-01 14:07:37 — Git tools installed
- Added Window → Aim2Pro → Git → Quick Actions (pull/commit/push/open/tag).
- Added pre-commit hook to block stray MenuItem paths.

## 2025-10-01 14:18:22 — Canonical Paste window added
- Window → Aim2Pro → Track Creator → Canonical Paste…
- Validate, Dry Run, Apply. Save/Load .sdplan. Creates Restore Point via menu.
- Last plan at StickerDash_Status/LastCanonical.plan

## 2025-10-01 14:33:43 — Fix Restore Point copy
- Ensure destination directories are created before copying (Create/Load).
- Fixes DirectoryNotFoundException for ProjectSettings files.
