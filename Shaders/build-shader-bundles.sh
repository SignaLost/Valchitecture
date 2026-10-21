#!/usr/bin/env bash
# Compiles Shaders/*.shader into one asset bundle per platform with a headless Unity
# editor and installs them under Resources/, where the mod build embeds them.
#
#   ./build-shader-bundles.sh
#
# Reads UNITY and PROJECT from build.local beside this script (not shipped), or from
# the environment. Unity must have been signed in through the Hub once.

set -u

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo="$(cd "$here/.." && pwd)"

[ -f "$here/build.local" ] && . "$here/build.local"
project="${PROJECT:-$HOME/valchitecture-shader}"

if [ -z "${UNITY:-}" ]; then
    # The game is on the 6000.0 stream; a bundle from a newer editor will not load.
    for candidate in "$HOME"/Unity/Hub/Editor/6000.0.*/Editor/Unity "$HOME"/Unity/Hub/Editor/*/Editor/Unity; do
        [ -x "$candidate" ] && UNITY="$candidate" && break
    done
fi

if [ -z "${UNITY:-}" ] || [ ! -x "$UNITY" ]; then
    echo "No Unity editor found. Put UNITY=/path/to/Editor/Unity in $here/build.local."
    exit 1
fi

echo "Editor:  $UNITY"
echo "Project: $project"

if [ ! -d "$project/Assets" ]; then
    "$UNITY" -batchmode -quit -nographics -createProject "$project" -logFile - || exit 1
fi

mkdir -p "$project/Assets/Shaders/Editor" || exit 1
rm -f "$project/Assets/Shaders"/*.shader "$project/Assets/Shaders"/*.shader.meta
cp "$here"/*.shader "$project/Assets/Shaders/" || exit 1
cp "$here/Editor/BuildGizmoShaderBundle.cs" "$project/Assets/Shaders/Editor/" || exit 1

log="$project/build.log"
"$UNITY" -batchmode -quit -nographics -projectPath "$project" \
         -executeMethod BuildGizmoShaderBundle.Build -logFile "$log"
status=$?
grep -E "^Built |^Standalone(Windows64|Linux64): " "$log"
[ $status -eq 0 ] || { echo "Unity exited with $status; see $log"; exit 1; }

mkdir -p "$repo/Resources" || exit 1
built=0

for pair in StandaloneLinux64:shaders_linux StandaloneWindows64:shaders_windows; do
    target="${pair%%:*}"
    resource="${pair##*:}"
    bundle="$project/ShaderBundles/$target/valchitecture_shaders"

    if [ -s "$bundle" ]; then
        cp "$bundle" "$repo/Resources/$resource" && echo "$target -> Resources/$resource" && built=$((built + 1))
    fi
done

[ "$built" -gt 0 ] || exit 1
echo "Done. Run dotnet build to embed them."
