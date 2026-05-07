#!/usr/bin/env bash
# Build dedicated Linux server (headless) via Unity batchmode.
#
# Usage:
#   ./Build/build-server.sh
#
# Wymaga: Unity 6.0.4.6f1 zainstalowane przez Unity Hub.
# Output: Build/Server-Linux/MmoGameServer.x86_64
#
# Konfiguracja przez env:
#   UNITY_PATH — pełna ścieżka do binarki Unity (jeśli nie auto-wykryta)
#   UNITY_VERSION — wersja, default 6000.4.6f1

set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BUILD_OUT="${PROJECT_ROOT}/Build/Server-Linux"
LOG_FILE="${PROJECT_ROOT}/Build/last-server-build.log"
UNITY_VERSION="${UNITY_VERSION:-6000.4.6f1}"

# Auto-detect Unity if not given
if [[ -z "${UNITY_PATH:-}" ]]; then
  case "$(uname -s)" in
    Darwin)
      UNITY_PATH="/Applications/Unity/Hub/Editor/${UNITY_VERSION}/Unity.app/Contents/MacOS/Unity"
      ;;
    Linux)
      UNITY_PATH="${HOME}/Unity/Hub/Editor/${UNITY_VERSION}/Editor/Unity"
      ;;
    *)
      echo "Set UNITY_PATH manually for this platform." >&2
      exit 1
      ;;
  esac
fi

if [[ ! -x "$UNITY_PATH" ]]; then
  echo "Unity nie znalezione w: $UNITY_PATH" >&2
  echo "Ustaw UNITY_PATH lub zainstaluj Unity ${UNITY_VERSION} przez Hub." >&2
  exit 1
fi

mkdir -p "$BUILD_OUT"
echo "→ Building Linux dedicated server with $UNITY_PATH"
echo "→ Output: $BUILD_OUT"
echo "→ Log:    $LOG_FILE"

"$UNITY_PATH" \
  -batchmode \
  -nographics \
  -quit \
  -projectPath "$PROJECT_ROOT" \
  -buildTarget StandaloneLinux64 \
  -executeMethod MmoGame.Editor.BuildScripts.BuildLinuxServer \
  -logFile "$LOG_FILE"

echo "✓ Server build done."
ls -lah "$BUILD_OUT"
