# Research: shelves, products, boxes (game v1.0.72)

Every product container (shelf slot, carton inside, player hands) holds ONE product Guid + count. The crate keeps its own list.

## Data model
- `ShelfProducts : Shelf` → `_places` (`ProductPricePlace[]`). All shelves: `AllServices.Get<AiService>()._shelves`.
- `ProductPricePlace` (price tag, NetworkBehaviour): `_productPlace`, `SyncVar<Guid> _productId`, `_lastDefinitionId`, `IsPlayerTakeBlocked`, server `TryTakeItemByPlayerServer(out Guid, out ProductTransferPayload)`.
- `ProductPlace` (NetworkBehaviour): `SyncVar<ProductStackState{Guid Id; int Count}> _productInPlace`, variant SyncLists (keychains/pins), `_availableProducts` (EProductType whitelist), `_maxCount`, `_items` (client visuals).
  - Checks: `CanPut(EProductType)`, `GetCapacity(ProductTypeSettings, def, item)`, `HasSameId(Guid)`, `Count`, `Id`.
  - Server: `TryAddItem(Guid, payload)`, `RemoveLastItemServer()`, `TryGetItem()` (payload, no removal), `SetItemsServer`.
  - Server move primitive (from carton coroutines): `var p = src.TryGetItem(); if (dst.TryAddItem(src.Id, p)) src.RemoveLastItemServer();`
- `PickupProducts : Pickup` (carton): `_productPlace`, `SyncVar<bool> _isOpened`; list in `ProductsService.ProductBoxes`.
- `ProductItem` = pooled non-networked visual. `PoolService.GetOrCreateProductPool(def).Get(parent)`, `SetProductInstancing(item,false)`, `ReturnProductItem`.
- `PickupDropped : Pickup` = networked single product on the floor.

## Ids
- Runtime id `Guid` (`ProductInfo.ProductId`), `ProductsService.GetProductInfo(Guid)`; definition `ProductsService._config.FindProductDefinition(int)` / `DataDefinition<ProductDefinition>.TryGetWithId`; `ProductHandsUtility.ResolveProductDefinition(productsService, Guid)`.
- `ProductsConfig.FindProductTypeSetting(EProductType)` → `MaxCountInHands`, etc.

## Player RPCs (PlayerCharacterController._productsController = PlayerProductsController)
- Take to hands: `AddProductItem(place)` → `RpcWriter___Server_AddProductItemRpc_4187530933(place, payload)`; server logic `RpcLogic___AddProductItemRpc_4187530933`.
- Put from hands: `AddItemToPlace(place)` → `RpcWriter___Server_AddItemToPlaceRpc_4187530933(place, payload)`; logic validates CanPut/HasSameId/GetCapacity then `ProductHandsUtility.TryAddToPlace`.
- Carton: `PlayerProductsBoxController.StartAddItemsToPlace/StartTakeItemsFromPlace` (server coroutines).
- Stock counting: `ProductsService.RefreshPhysicalStockCounts` / `AddPhysicalStock(Guid, count)`.

## Interaction states
- Empty hands `PlayerHandsInteractionsState`, products in hands `PlayerCarryItemsInteractionsState`, carton `PlayerPickupInteractionsState`.
- `AddItemToProductPlaceInteraction` / `TakeItemFromProductPlaceInteraction`: raycast `PlayerRaycastController.Interactable` → `ProductPlace` (or `ProductPricePlace`).
- Aimed slot pattern (Plus mod): `player._raycastController.Interactable?.TryCast<ProductPricePlace>()` / `TryCast<ProductPlace>()`; skip when cursor enabled.

## Visuals
- Hands: `PlayerProductsController.MoveProductInHands`, `_fpsPickupSlot` / `_tpsPickupSlot`, `HandSettings` offsets.
- Crate contents: pooled `ProductItem`s parented to crate, instancing off, interactable off, scaled down.
