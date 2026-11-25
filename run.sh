#!/usr/bin/env bash
# Build the mod and launch a game copy with it loaded.
#
#   ./run.sh [VERSION] [game args...]
#
# With no VERSION, builds against and launches ../base_game/ (the normal dev setup).
# VERSION is a folder name in ../versions/ (e.g. 1.23.0-pre.1). The mod is then
# compiled against that version's DLLs into UsableSlabs/bin/compat/VERSION/ and launched
# in that game copy with its own data dir (../versions/data/VERSION), so a newer
# version can't migrate your main saves or settings.
# Remaining arguments go to the game, e.g. ./run.sh 1.22.0 --openWorld "test"
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VERSIONS="$(realpath -m "$ROOT/../versions")"

VERSION=""
if [ $# -gt 0 ] && [[ "$1" != -* ]]; then
  if [ -d "$VERSIONS/$1" ]; then
    VERSION="$1"; shift
  else
    echo "Unknown version '$1'. Available:" >&2
    find "$VERSIONS" -mindepth 1 -maxdepth 1 -type d ! -name data -printf '  %f\n' 2>/dev/null | sort >&2
    exit 1
  fi
fi

if [ -z "$VERSION" ]; then
  GAME="$(cd "$ROOT/../base_game" && pwd)"
  OUT="$ROOT/UsableSlabs/bin/Debug/Mods"
  dotnet build "$ROOT/UsableSlabs/UsableSlabs.csproj"
  DATA_ARGS=()
else
  GAME="$(cd "$VERSIONS/$VERSION" && pwd)"
  OUT="$ROOT/UsableSlabs/bin/compat/$VERSION"
  dotnet build "$ROOT/UsableSlabs/UsableSlabs.csproj" \
    -p:VINTAGE_STORY="$GAME" \
    -p:OutputPath="$OUT/mod/"

  DATA="$VERSIONS/data/$VERSION"
  # Copy client settings (login, graphics, keybinds) on first run so you don't have to log in again.
  if [ ! -d "$DATA" ]; then
    mkdir -p "$DATA"
    SRC="${XDG_CONFIG_HOME:-$HOME/.config}/VintagestoryData/clientsettings.json"
    [ -f "$SRC" ] && cp "$SRC" "$DATA/"
    mkdir -p "$DATA/Mods"
  fi
  # clientsettings.json stores modPaths with an absolute path to the data dir's Mods
  # folder; point it at this data dir so mods from the main install aren't picked up.
  if [ -f "$DATA/clientsettings.json" ]; then
    python3 -I - "$DATA/clientsettings.json" "$DATA/Mods" <<'PY'
import json, sys
path, mods = sys.argv[1], sys.argv[2]
with open(path) as f:
    d = json.load(f)
def fix(o):
    if isinstance(o, dict):
        for k, v in o.items():
            if k == "modPaths":
                o[k] = ["Mods", mods]
            else:
                fix(v)
fix(d)
with open(path, "w") as f:
    json.dump(d, f, indent=2)
PY
  fi
  DATA_ARGS=(--dataPath "$DATA")
fi

cd "$GAME"
# Other mods to test against (e.g. other mods) go in <game>/ExtraMods;
# <game>/Mods is reserved for the game's own mods.
MOD_PATHS=("$OUT")
[ -d ExtraMods ] && MOD_PATHS+=("$PWD/ExtraMods")

exec dotnet Vintagestory.dll --tracelog \
  "${DATA_ARGS[@]}" \
  --addModPath "${MOD_PATHS[@]}" \
  --addOrigin "$ROOT/UsableSlabs/assets" \
  "$@"
