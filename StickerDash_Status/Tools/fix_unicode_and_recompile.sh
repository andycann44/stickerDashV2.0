#!/usr/bin/env bash
set -euo pipefail
echo "[fix] Sweeping Assets/*.cs for unicode × (U+00D7), literal \\u00D7, smart quotes, dashes, degree sign…"
find Assets -name "*.cs" -print0 | xargs -0 perl -CS -i -pe \
  's/\x{00D7}/x/g; s/\\u00D7/x/g; s/\x{2013}/-/g; s/\x{2014}/-/g; s/\x{2018}/\x27/g; s/\x{2019}/\x27/g; s/\x{201C}/"/g; s/\x{201D}/"/g; s/\x{00B0}/ deg /g; s/\x{FEFF}//g; s/\x{200B}//g; s/\x{200C}//g; s/\x{200D}//g;'

echo "[fix] Collapsing any by|x|× → by|x"
LC_ALL=C find Assets -name "*.cs" -print0 | xargs -0 sed -i  -e $s/by
