# Track Generator — Single Source of Truth
**Date:** 2025-10-04

- One editor window only: **Window → Aim2Pro → Track Creator → Track Lab (All-in-One)**.
- No renames. No new windows. Goal posts fixed.

## Baseline behaviours
- Create grid: length in meters → rows; width in tiles; tiles touch; tile size 1m; thickness 0.2m.
- Straighten rows A–B: aligns X to previous row and matches tile heights.
- Offset rows A–B: X (lateral) or Y (height).
- Append straight: length L with step s (rows).
- Delete: row N or rows A–B.

## Next (add one at a time)
1. Smooth altitude (no stepping).
2. Curves/chicanes with degree control.
3. Rule: no gaps in first 10 rows (validator).
