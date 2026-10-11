# Research: game UI (prompts, warnings, panels) — game v1.0.72

## Key prompts (right side)
- `PlayerUIController.ShowControlKey(InputAction)` / `HideControlKey(InputAction)` (local player's `_uiController`). Keyed by the InputAction reference.
- `new InputAction(InputEnum, "", meanKey)` is a plain object. Key text/sprite come from Rewired for `InputEnum` (mouse buttons/wheel → `InputConfig._leftMouseSprite/_rightMouseSprite/_wheelMouseSprite`).
- `GameView.CreateControlKey` dedups by `(MeanKey, Key)` → always use own meanKeys (prefix `ASC/`).
- `ControlKeyWidget.OnActivate` sets `_meanText.text = LocalizationManager.GetTranslation(Mean)` → postfix it to put literal text for `ASC/` keys.
- Keyboard map (Plus GameKeys): E=Hand, R=Toggle, G=Drop, F=Decor/Sorting, C=Remove, Q=Inventory; Use/Back = LMB/RMB, RotateHorizontal = mouse wheel.

## Warnings (centre)
- `GameView._warningWidget` (`WarningWidget`): `_text.text = …; SpawnWarning()` (clones the template, plays "button_buy_wrong"). `UIService.ShowWarning(term)` for localization terms.

## Styling
- `GameView`: `_levelText`, `_questTitleText`, … (TMP font + material), panels `_questPanel`, `_tutorialPanel` (Image sprite/colour).
- Parent custom uGUI under the GameView so it inherits its CanvasScaler and hides with the HUD.
- `ProductDefinition.Icon` sprite, `.Name` localized.
