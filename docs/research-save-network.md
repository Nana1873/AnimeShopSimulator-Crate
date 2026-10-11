# Research: saves, networking, players (game v1.0.72)

Findings from Cpp2IL ISIL + Il2CppInterop stubs. Hashed RPC suffixes change between game builds.

## Saves
- `Il2CppProject.Code.Core.Saves.SaveService` via `AllServices.Get<SaveService>()`.
- Files: `%USERPROFILE%/AppData/LocalLow/OneMoreTime/Anime Shop Simulator/<slot>_save.es3` (plain ES3 JSON, keys `sessionSave`, `sessionSaveTransactionId`). `globalSave.es3` → `SlotPreviews[slot].transactionId`.
- Only the host writes (`HasSessionWriteAuthority`).
- Save flow: `TryPrepareSessionSave` (main thread, calls every `ISaveComponent.Save()`) → `ExecutePreparedSessionSave` → `SavePreparedSessionToDisk` → `CompleteSave` → `RecordCommittedTransaction(int slotNumber, string transactionId)`.
- Load: `SaveService.Load(slot)` sets `SlotNumberActive`; world restore in `GameController.BootstrapSceneBuildingPieces`, `OrderController.SpawnFromDatas`; `GameStartupService.EndStartup` / `OnStartupStateChanged(false)`.
- Plan: sidecar `UserData/AnimeShopCrate/<slot>.json`.
  - Snapshot in a postfix of an `ISaveComponent.Save()` (e.g. `OrderController.Save`) → `<slot>.pending.json`.
  - Commit in postfix of `SaveService.RecordCommittedTransaction` → stamp transaction id, rename to `<slot>.json`.
  - Load on host after startup, verify transaction id (`TryGetCommittedTransactionId`). Clean up on `TryDelete` / `CreateNewSessionForSlot`.
  - Do not add keys to the ES3 file (validated, cached writes).
- Dropped boxes are saved as `OrderSaveData` (`IsDroppedPickup`, position, `Guid ProductId`, count, variant lists).
- Product ids are `Il2CppSystem.Guid`. `ProductInHand {Guid ProductId, int Count, List<int> VariantDefinitionIds, VariantKeychainFoils, List<string> VariantKeychainIds}`.

## Networking (mod-to-mod messages)
- Custom FishNet broadcast struct: not possible (IL2CPP generated serializers).
- **Preferred:** register own RPC hash on an existing NetworkBehaviour: `RegisterServerRpc(uint, ServerRpcDelegate)`, `RegisterObserversRpc`, `RegisterTargetRpc`, `SendServerRpc(uint, PooledWriter, Channel, DataOrderType)`, `SendObserversRpc(...)`, `SendTargetRpc(...)`. Writer: `WriterPool.Retrieve()`, `WriteString`/`WriteArraySegmentAndSize`, `Store()`. Hash width depends on behaviour RPC count; pick small unused hash. Untested.
- Fallback A: piggyback `PlayerAnimationController` PlayEmoji server/observers string RPCs with a magic prefix.
- Fallback B: Steam P2P (`Il2CppSteamworks.SteamNetworking.SendP2PPacket` / `ReadP2PPacket`, `SteamNetworkingMessages`). FishySteamworks only uses `SteamNetworkingSockets`.
- Fallback C: Dissonance `DissonanceFishNetBroadcast` (ArraySegment<byte> payload), intercept `OnDissonanceDataReceived`.

## Players / roles
- `NetworkService.IsServerInitialized` (host), `IsClientInitialized`, `IsLocalClientHost`.
- Local player: `AllServices.Get<PlayerCharacterRegistryService>()._players` where `IsOwner`.
- SteamID: `PlayerCharacterController.SteamId` (SyncVar<ulong>), `ConnectionService.PlayerId(conn)`.
- Events: `NetworkService.OnPlayerConnected/OnPlayerDisconnected/OnPlayerLoadedStartScenes`; `GameController.NotifyPlayerLocalInitializationCompleted(conn)` → send snapshot.
- Carrying: `PlayerPickupController` (`SyncVar<Pickup> _pickup`, hand sockets, `Pickup.PickInHands`).

## Spawning
- `NetworkService.SpawnNetworkObject<T>(prefab, pos, rot, parent, owner, beforeSpawn)`, `DespawnNetworkObject`.
- Boxes: `OrderController.CreatePickup` with `ProductsConfig.FindPickupDefinition(id).BoxPrefab`. Reusing a box prefab drags in Pickup logic (AI targeting, order saves).
- Plan: crate world visual is a **local, non-networked** object driven by mod sync messages.
