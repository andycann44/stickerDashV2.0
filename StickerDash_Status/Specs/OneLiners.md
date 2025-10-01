# One-Liners I Use

## Direct push to main
git add -A && git commit -m "chore: update" || true && git push origin main

## Branch + PR
BR=feat/quick; git checkout -B "" && git add -A && git commit -m "feat: quick" || true && git push -u origin "" && gh pr create -B main -t "feat: quick" -b "details"

## Fix LFS push issues
git lfs install && git push origin main
