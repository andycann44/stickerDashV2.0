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
