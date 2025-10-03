\n\n## Status Snapshot — 2025-10-02

**Build state:** ✅ Editor scripts compile cleanly.

**What you have now**
- Single-source NL engine: `Assets/StickerDash/AIGG/Editor/Track/NLEngine.cs` (minimal, ASCII-only).
- NL menus:
  - Window → Aim2Pro → Track Creator → **NL Tester**
  - Window → Aim2Pro → Track Creator → NL → **Parse From File / Run From File / Run From File (SBend Fix)**
- Behavior:
  - `ParseNL(...)` currently returns a safe canonical plan:
    ```
    seed(1)
    buildAbs(100,3)
    safeMargin(5)
    ```
  - `WritePlan(...)` writes to `StickerDash_Status/LastCanonical.plan`.
  - `RunPlan(...)` triggers the existing Track Gen menu (normal or SBend Fix).

**How to use**
1. Open **NL Tester**, click **Parse Only** → verify canonical.
2. Click **Write Plan** → file appears at `StickerDash_Status/LastCanonical.plan`.
3. Click **Run Plan** → generator runs with that canonical.

**Known limitation (temporary)**
- NL parsing is stubbed to guarantee a green compile. It does **not** transform free text yet; it just emits a small, valid plan.

**Next steps (proposed)**
1. Re-enable these NL phrases → canonical (ASCII-only):
   - `build L by W` → `buildAbs(L,W)`
   - `safe margin N` / `safe start N` / `safe end N`
   - `random holes PCT`
2. Add: `s bend A-B at DEG gain G`.
3. Add: `random slopes MIN to MAX, segment N`.
(Commit each step with specs + tests.)\n

## NL Amendments — shipped 2025-10-02

You can now type NL (plain English) to **rebuild** or **amend** without editing `LastCanonical.plan`:

### Rebuild (fresh plan)
- `rebuild 220 by 3 seed 11`
- `build 200 by 3` (seed auto if random ops exist)
- `safe start 10 end 10`
- `random slopes 2 to 4 degrees, segment 2`
- `auto s-bends 2 at 25`

### Amend existing plan (append/modify)
- Remove: `remove rows 80 to 120` · `remove tiles 2,4,7 in row 95`
- Shape: `curve rows 40 to 60 left 20 degrees` · `s bend 60 to 120 at 25 gain 2`
- Random features: `random holes 5%` · `add 2 jump gaps`
- Smooth: `smooth heights`
- Fork / merge: `fork 120 to 160 widen to 3` · `rejoin 200 to 240`
- Protect: `protect start 10 end 10`

### Fill / Solid (plan-level undo of deletes)
- `fill row 120` · `fill rows 80 to 120` · `fill tiles 2,3 in row 95`
- `solid track` / `fill all` — removes global delete ops (holes, jump gaps, deleteRows/tiles) from the base plan.

> Note: Fill operates by editing the **base plan text**: it drops matching `deleteRows(...)`, `deleteTiles(..., row=R)`, and global delete ops (for **fill all**). It does not reverse random deletions that are not explicitly listed (e.g., `randomHoles`) unless you use **fill all**.

### Macros (expanded automatically on write)
- `smoothHeights(both, tolerance=0.02)` → `smoothColumns()`
- `ySplit(a,b, gapStart=1, gapEnd=3)` — center divider widening
- `yMerge(a,b, toGap=0)` — shrink the divider to rejoin

### Auto-seed capture
If your plan uses random ops and no `seed(...)`, a seed is auto-inserted and logged to `StickerDash_Status/Seeds.log` so later amendments keep the same randomness.

### Menus
- Window → Aim2Pro → Track Creator → **NL Tester** (type NL, Parse/Write/Run)
- Window → Aim2Pro → Track Creator → NL → **Parse From File / Run From File**


## Changelog 2025-10-02
- Fix: removed Tuple<> usage in NLEngine and replaced with simple structs to resolve CS1001.
- Added System.Collections.Generic using and simplified fill/amend helpers.


## Changelog 2025-10-02
- Hotfix: replaced NLEngine.cs with a lean compile-safe version (rebuild + amend + smooth/fork/merge macros + auto-seed).
- Temporarily deferred fill/solid to next patch after we confirm green compile.


## Changelog 2025-10-02
- Fixed PlanMacros to avoid char literal \\n; now uses StringReader for line iteration (more robust on all platforms).


## Changelog 2025-10-03
- Added **NL Tester** window: Window → Aim2Pro → Track Creator → NL Tester.
- Added **NL → Parse From File / Run From File / Open Cheat Sheet** menus.
- Created **StickerDash_Status/NL_CheatSheet.md** with current NL commands and usage.


## Changelog 2025-10-03
- **PlanMacros updated:** safe margins are now preprocessed (no warnings).
  - `safeMarginStart(N)` / `safeMarginEnd(N)` lines are **not** written to the final plan.
  - Any `deleteRows(a,b)` is clipped to avoid the protected first/last rows.
  - Any `deleteTiles(..., row=R)` on protected rows is dropped.
  - `smoothHeights(...)` and legacy `smoothColumns()` are **silently dropped** (placeholder until real smoother op exists).
- **Limit:** Random-wide ops (e.g., `randomHoles`) still affect protected rows (generator limitation). Use explicit deletes + margins when you need hard-safe ends.


## Changelog 2025-10-03
- **Menu cleanup:** kept only *NL → Open Cheat Sheet*. *Parse/Run From File* moved into **NL Tester** window.
- **NL Tester** now has buttons: *Open Cheat Sheet*, *Open NL.input*, *Parse From File*, *Run From File*.


## Changelog 2025-10-03
- **Cheat Sheet now opens inside Unity** (viewer window with Refresh / Open Externally).
- Added a **“+” insert button** in NL windows that pops a menu of ready-made commands
  (including *chicane left→right* and *right→left* examples). Clicking inserts text into the NL box.
- File tools (Open NL.input / Parse From File / Run From File) remain accessible from the same bar.
