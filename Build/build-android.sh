#!/usr/bin/env bash
# Build Android client APK via Unity batchmode.
#
# Usage:
#   ./Build/build-android.sh
#
# Wymaga: Unity Android Build Support module zainstalowany przez Hub,
#         JDK + Android SDK/NDK skonfigurowane w Unity Preferences.
# Output: Build/Client-Android/MmoGame.apk

set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BUILD_OUT="${PROJECT_ROOT}/Build/Client-Android"
LOG_FILE="${PROJECT_ROOT}/Build/last-android-build.log"
UNITY_VERSION="${UNITY_VERSION:-6000.4.6f1}"

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
  exit 1
fi

mkdir -p "$BUILD_OUT"
echo "→ Building Android APK with $UNITY_PATH"

"$UNITY_PATH" \
  -batchmode \
  -nographics \
  -quit \
  -projectPath "$PROJECT_ROOT" \
  -buildTarget Android \
  -executeMethod MmoGame.Editor.BuildScripts.BuildAndroidClient \
  -logFile "$LOG_FILE"

echo "✓ Android build done."
ls -lah "$BUILD_OUT"
