# Anime Shop Crate

MelonLoader mod for **Anime Shop Simulator** (IL2CPP, game v1.0.72): a crate tool like the one in Supermarket Simulator. Pick it from the Q wheel (slot 6, or key 6), collect mixed products from shelves and opened boxes, put them onto shelves, and set the crate down when you need your hands. Co-op ready (host-authoritative), crates persist in the save.

## Status
- [x] Step 1: Q wheel slot + crate in hand (visible to other players via the game's tool-mode sync)
- [x] Step 2: take products into the crate / place them from the crate (host-authoritative)
- [x] HUD list of crate contents (count + space)
- [x] Multiplayer tested: guest/host take & put, simultaneous takes, guest crate saved by host
- [x] Third-person crate pose (in front of the body)
- [ ] Step 3: products visible inside the crate
- [ ] Step 4: set the crate down / pick it up
- [x] Step 5: save/load of crate contents (sidecar file, transaction-checked)
- [ ] Count crate contents as stock for reordering
- [ ] Real crate model

## Build
```
dotnet build -c Release
```
Set `GameDir` if the game is not at `E:\SteamLibrary\steamapps\common\Anime Shop Simulator`. `-p:Deploy=true` copies the DLL into `Mods`.

Both players need the same mod version.

## Settings
`UserData/MelonPreferences.cfg`, section `[AnimeShopCrate]`: debug log, crate offsets in first/third person, crate scale.

## Research notes
See `docs/` (game internals found with Cpp2IL).
