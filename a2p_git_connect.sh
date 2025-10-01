#!/usr/bin/env bash
set -euo pipefail
if [ $# -lt 1 ]; then
  echo "Usage: $0 <remote-url>"
  echo "  Examples:"
  echo "    $0 git@github.com:YOURUSER/stickerdash2.git"
  echo "    $0 https://github.com/YOURUSER/stickerdash2.git"
  exit 1
fi
REMOTE="$1"
# set main as default branch if needed
git symbolic-ref -q HEAD >/dev/null || git checkout -b main
git branch -M main
if git remote get-url origin >/dev/null 2>&1; then
  git remote set-url origin "$REMOTE"
else
  git remote add origin "$REMOTE"
fi
git push -u origin main
echo "Connected to: $REMOTE"
