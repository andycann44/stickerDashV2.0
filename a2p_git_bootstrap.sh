#!/usr/bin/env bash
set -euo pipefail

ROOT="$(pwd)"
[ -d "$ROOT/Assets" ] && [ -d "$ROOT/ProjectSettings" ] || { echo "ERR: run at Unity project root"; exit 1; }

echo "== Aim2Pro: Git bootstrap for Unity =="

# 0) Recommend Unity settings (manual but important)
echo "TIP: In Unity → Edit → Project Settings → Editor:"
echo "     - Version Control: Visible Meta Files"
echo "     - Asset Serialization: Force Text"

# 1) Init repo if needed
if [ ! -d ".git" ]; then
  git init
  echo "Initialized empty Git repo."
else
  echo "Git repo already present."
fi

# 2) Unity .gitignore (official base + a few extras)
if [ ! -f ".gitignore" ]; then
cat > .gitignore <<'GI'
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Mm]emoryCaptures/
UserSettings/
.uvprojx
.uvoptx
*.csproj
*.pidb
*.suo
*.user
*.userprefs
*.unityproj
*.booproj
*.svd
*.pdb
*.mdb
*.opendb
*.VC.db
*.swp
*.sln
*.tmp
*.TMP
*.tmp.meta
*.TMP.meta
*.orig
*.orig.meta

# Rider/VS caches
.idea/
.vscode/
*.DotSettings.user

# Addressables
/Assets/AddressableAssetsData/*/*.bin*
/Assets/AddressableAssetsData/*/*.hash
/Assets/AddressableAssetsData/*/*_data.bin*

# Cache
/[Aa]ssets/StreamingAssets/aa/*
/[Bb]ackup*/
# Keep meta files
!/[Aa]ssets/**/*.meta

# Stickerdash project status (keep)
!/StickerDash_Status/
GI
  echo "Wrote .gitignore"
else
  echo ".gitignore already exists (kept)."
fi

# 3) Git LFS — track big binaries commonly used in Unity projects
if command -v git >/dev/null 2>&1; then
  if ! git lfs version >/dev/null 2>&1; then
    echo "Git LFS not found."
    if command -v brew >/dev/null 2>&1; then
      echo "Attempting to install Git LFS via Homebrew..."
      brew install git-lfs || true
    else
      echo "Install Git LFS manually (https://git-lfs.com) and run: git lfs install"
    fi
  fi
fi

git lfs install || true

# Track common binary formats
cat > .gitattributes <<'GA'
# 3D / Animation
*.fbx filter=lfs diff=lfs merge=lfs -text
*.obj filter=lfs diff=lfs merge=lfs -text
*.blend filter=lfs diff=lfs merge=lfs -text

# Textures & images
*.psd filter=lfs diff=lfs merge=lfs -text
*.png filter=lfs diff=lfs merge=lfs -text
*.jpg filter=lfs diff=lfs merge=lfs -text
*.jpeg filter=lfs diff=lfs merge=lfs -text
*.tga filter=lfs diff=lfs merge=lfs -text
*.tif filter=lfs diff=lfs merge=lfs -text
*.tiff filter=lfs diff=lfs merge=lfs -text
*.exr filter=lfs diff=lfs merge=lfs -text

# Audio / Video
*.wav filter=lfs diff=lfs merge=lfs -text
*.mp3 filter=lfs diff=lfs merge=lfs -text
*.aiff filter=lfs diff=lfs merge=lfs -text
*.ogg filter=lfs diff=lfs merge=lfs -text
*.mp4 filter=lfs diff=lfs merge=lfs -text
*.mov filter=lfs diff=lfs merge=lfs -text
GA

# 4) Basic README
if [ ! -f README.md ]; then
cat > README.md <<'RD'
# StickerDash2.0

- Unity project under Git + LFS.
- Defaults: meters unless stated; degrees for curves/slopes.
- Menus live under Window → Aim2Pro.

## First-time
- In Unity: Edit → Project Settings → Editor
  - Version Control = Visible Meta Files
  - Asset Serialization = Force Text

## Working Notes
See `StickerDash_Status/Notes/SD2_WorkingNotes.md`
RD
fi

# 5) Working Notes entry
mkdir -p StickerDash_Status/Notes
{
  echo ""
  echo "## Git Setup $(date +%Y-%m-%d' '%H:%M:%S)"
  echo "- Initialized Git, added Unity .gitignore, configured Git LFS."
} >> StickerDash_Status/Notes/SD2_WorkingNotes.md

# 6) First commit (safe if there are already commits)
git add .
if git diff --cached --quiet; then
  echo "Nothing new to commit."
else
  git commit -m "chore: bootstrap git + unity .gitignore + LFS"
fi

# 7) Helper script: connect to GitHub (takes remote URL as arg)
cat > a2p_git_connect.sh <<'CON'
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
CON
chmod +x a2p_git_connect.sh

echo "== Done =="
echo "Next: create a repo on GitHub, then run:"
echo "  ./a2p_git_connect.sh <your-remote-url>"
echo "For SSH setup help, run:  bash a2p_git_ssh_help.sh  (created below)."

# 8) SSH helper (prints your pubkey or generates one)
cat > a2p_git_ssh_help.sh <<'SSH'
#!/usr/bin/env bash
set -euo pipefail
EMAIL_DEFAULT="$(git config user.email || true)"
echo "== SSH setup for GitHub =="
if [ -f "$HOME/.ssh/id_ed25519.pub" ]; then
  echo "Public key exists at: $HOME/.ssh/id_ed25519.pub"
else
  echo "No SSH key found. Generating ed25519 key..."
  read -p "Email for SSH key [$EMAIL_DEFAULT]: " EMAIL
  EMAIL=${EMAIL:-$EMAIL_DEFAULT}
  ssh-keygen -t ed25519 -C "$EMAIL"
  eval "$(ssh-agent -s)"
  ssh-add "$HOME/.ssh/id_ed25519"
fi
echo ""
echo "== Your public key =="
cat "$HOME/.ssh/id_ed25519.pub"
echo ""
echo "Copy the above key, then go to: https://github.com/settings/keys → New SSH key → paste → Save."
echo "After adding, test: ssh -T git@github.com"
SSH
chmod +x a2p_git_ssh_help.sh

