# Usable Slabs

Vintage Story code mod (modid `usableslabs`, C#, net10.0). Layout matches the sibling `../cook_ui` repo.

## Layout
- `UsableSlabs/` — the mod: `modinfo.json`, `UsableSlabsModSystem.cs`, `assets/usableslabs/` (lang: `en`, `es-419`).
- `ZZCakeBuild/` — Cake packaging: `./build.sh` validates asset JSON, publishes Release, writes `Releases/usableslabs_<version>.zip`.
- `Directory.Build.props` — `VINTAGE_STORY` defaults to `../base_game` (shared local game copy, not versioned).
- `.github/workflows/release.yml` — pushing tag `vX.Y.Z` builds against the server package and creates a GitHub release. The tag must equal `version` in `modinfo.json`.

## Running / testing
- `./run.sh` builds into `UsableSlabs/bin/Debug/Mods` and launches `../base_game` with the mod and `--addOrigin` on the assets folder.
- `./run.sh <VERSION>` builds against `../versions/<VERSION>/` into `UsableSlabs/bin/compat/<VERSION>/` and launches with an isolated data dir `../versions/data/<VERSION>`.
- Other mods to test against go in `<game>/ExtraMods` (not `<game>/Mods`, which holds the game's own mods).
- The user often keeps the game running while asking for changes. Vintage Story can't hot-reload code mods, and overwriting the loaded DLL can crash it. Check with `pgrep -f '^dotnet Vintagestory.dll'` (a bare `pgrep -f Vintagestory.dll` matches the checking shell itself); if running, verify compilation out of tree: `dotnet build UsableSlabs/UsableSlabs.csproj -p:OutputPath=<scratch>/out/`. Then the user quits and reruns `./run.sh` themselves. Never kill the game without asking.

## Conventions
- Prefer reading game behaviour from the decompiled/vanilla sources and DLLs in `../base_game` over guessing; compare against `../versions/*` when targeting other game versions.
- Log with a `[usableslabs]` prefix.
- Keep every user-facing string in the lang files; add both `en` and `es-419`.
- Release: bump `version` in `modinfo.json` in its own commit ("Bump version to X.Y.Z"), then tag `vX.Y.Z`.
- An untracked `pending.md` at the repo root may hold a hand-off from a previous session; read it first if present.
