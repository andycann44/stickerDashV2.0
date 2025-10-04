# Track Generator — Single-Source of Truth (Baseline)
**Date:** 2025-10-04

## Decision
- Keep **one** editor window only: **Window → Aim2Pro → Track Creator → Track Lab (All-in-One)**.
- All other Track-Gen windows are **disabled** (compiled out with `#if false`).

## What works now (baseline)
- `create <L> m by <W> m` → builds L×W grid (tiles touch; default tile=1m, thickness=0.2m).
- `straighten rows A-B` → align X and heights to previous row (no stepping).
- `offset rows A-B x <m>` → shift rows sideways.
- `offset rows A-B y <m>` → raise/lower tile heights for rows.
- `append straight <L> m [step <s>]` → extend track by length L.
- `delete row N` · `delete rows A-B` · `delete tiles row N: 1,3,5`.

## Next steps (add one at a time)
1) Smooth altitude changes (bezier/spline) — avoid stepping when changing height.
2) Curves/chicanes (AppendArc/CurveRows) with degree control and width preservation.
3) Rule: **no gaps** in first 10 rows (validator + fixer).
4) Randomizers (lightweight): missing tiles %, soft bends up to N°, gentle ups/downs.
5) Persist plan to `StickerDash_Status/LastCanonical.json`.

## Acceptance Checklist (per change)
- [ ] Compiles cleanly; menu appears exactly once.
- [ ] `create 120 m by 6 m` yields a clean grid; tiles touch.
- [ ] Edits apply without exceptions; Scene remains interactive.
- [ ] Log shows normalized input → matched rule → effect count.
- [ ] No new windows/menus added.
