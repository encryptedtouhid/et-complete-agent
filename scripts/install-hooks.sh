#!/usr/bin/env bash
# Point this repo's git at the tracked hooks in .githooks/.
# Run once after every fresh clone.
set -euo pipefail
REPO_ROOT=$(git rev-parse --show-toplevel)
cd "$REPO_ROOT"
chmod +x .githooks/*
git config core.hooksPath .githooks
echo "✓ Hooks installed. core.hooksPath = .githooks"
echo
echo "  pre-commit  → pack + install + generate + build + test"
echo
echo "  Bypass with: git commit --no-verify"
