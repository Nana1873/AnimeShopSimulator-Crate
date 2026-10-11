# Anime Shop Simulator – Crate

A MelonLoader mod for **Anime Shop Simulator** that adds a shopping crate, like the one in Supermarket Simulator. Take it from the Q wheel, collect mixed products from your shelves, carry them to another shelf and put them back, or set the crate down when you need your hands.

Works in single player and co-op (both players need the mod).

> **Status: beta.** Tested on game version 1.0.72 in single player and co-op. Standing crates, the crate model and throwing products away are not yet tested in co-op.

## Features

- **Q wheel slot 6** (or key **6**) gives you the crate; key **1** or the hand slot puts it away.
- **Right click** a shelf slot: take one product into the crate. Hold to take more.
- **Left click** a shelf slot: put the selected product onto it, also onto empty slots. Hold to repeat.
- **Mouse wheel**: choose which product to put next. The selected product is outlined in the crate.
- **Left click a trash can**: throw the selected product away (follows the lobby's "throw products away" option).
- **Mixed contents** shown inside the crate, sorted into stacks per product.
- **Space**: 48 points. Pins, keychains, boosters 1 · manga, pin plates 2 · mini dakimakura, pin and keychain boxes 3 · figures, booster boxes, dresses 4 · figure boxes 6 · big booster boxes, cushions 8 · dakimakura 12. Cards do not go into the crate.
- **Set the crate down** with the game's box-placing key (E by default): a placement preview like the game's boxes, left click to place, right click to cancel, mouse wheel to rotate. Look at it and press E again to pick it up.
- **One crate per player.** While yours stands somewhere you cannot take a new one, and only you can pick yours up.
- **Saved with your game**, including standing crates and your co-op partner's crate.
- **Counts as stock**: products in crates still count for the order computer and reorder helpers.
- **Game-style UI**: native key prompts, warnings and a crate panel with product icons.
- **Translated** into all of the game's languages – English, German, French, Spanish, Italian, Portuguese, Russian, Polish, Czech, Swedish, Turkish, Vietnamese, Japanese, Korean and Chinese – plus Ukrainian and traditional Chinese, following the game's language.
- While you hold the crate, the things the game's tools cannot do are blocked too (computer, register, battle table, wardrobe, moving or packing furniture).

## Installation

1. Install [MelonLoader](https://melonwiki.xyz) 0.7 or newer for Anime Shop Simulator.
2. Download `AnimeShopCrate.dll` from the [Releases](../../releases) page.
3. Put it into the game's `Mods` folder.

In co-op, every player needs the same version of the mod. The host's crate space setting counts.

## Settings

`UserData/MelonPreferences.cfg`, section `[AnimeShopCrate]`:

| Setting | Default | What it does |
|---|---|---|
| `CrateSpace` | 48 | Crate space in points (host's value counts) |
| `DropKey` | `Game` | Key to set the crate down / pick it up. `Game` uses the game's box-placing key |
| `CrateScale` | 1 | Size of the crate model |
| `FirstPersonOffsetX/Y/Z`, `FirstPersonTiltX` | 0 | Shift and tilt of the crate in your own view |
| `ThirdPersonOffsetX/Y/Z`, `ThirdPersonTiltX` | 0 | Shift and tilt of the crate on other players |
| `DebugLog` | false | Extra diagnostics in the MelonLoader log and `UserData/AnimeShopCrate/` |

Crate contents are saved next to the game's save in `UserData/AnimeShopCrate/slot<N>.txt`.

## After a game update

On start the mod reports in the MelonLoader log which game version it was built for and how many game methods each part could hook. If a critical part (crate mode, network, save) cannot hook into the game anymore, the crate turns itself off for that session and says so. Your saved crates stay untouched; look for a mod update.

## Building

Requirements: .NET SDK 6 or newer, the game with MelonLoader installed (run the game once so MelonLoader generates the `Il2CppAssemblies`).

1. Copy `local.props.example` to `local.props` and set `GameDir` to your game folder.
2. `dotnet build -c Release` (add `-p:Deploy=true` to copy the DLL into `Mods`).

The crate model is in `assets/crate.blend`; `assets/crate.obj` is embedded into the DLL. `docs/` contains notes on the game internals the mod uses.

## License

MIT, see [LICENSE](LICENSE).
