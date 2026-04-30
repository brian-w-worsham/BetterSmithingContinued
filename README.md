# Better Smithing Continued

A Mount & Blade II: Bannerlord mod that removes the smithing stamina bottleneck so you can refine, smelt, and craft as long as your materials hold out — and recovers stamina faster when it does run down. Inspired by [Better Smithing Continued](https://www.nexusmods.com/mountandblade2bannerlord/mods/4318) by Aragas / OliverK / community contributors on Nexus Mods.

> This is a lightweight, vanilla-only re-implementation focused on the gameplay tuning and naming-quality-of-life that the original mod provided. It does **not** ship the original mod's full UI overhaul — specifically: saved weapon designs, hotkey-driven batch refine/smelt/craft, character-cycle hotkeys, smart smelt-all, group-identical-crafted-weapons in inventory, in-game settings menu, or skill-icon coloring. Those features all require ButterLib + MCM + UIExtenderEx + custom Gauntlet XML, which this rewrite intentionally avoids. If you want them, install the original mod from Nexus alongside its `Bannerlord.Harmony` / `Bannerlord.ButterLib` / `Bannerlord.MBOptionScreen` dependencies.

## What it does

| Feature | Default | Description |
| --- | --- | --- |
| Unlimited crafting stamina | **On** | Refining, smelting, and smithing all cost zero stamina. Your hero can keep working as long as you have materials. |
| In-town stamina recovery multiplier | **5×** | Multiplies the hourly stamina recovery rate while the hero's party is inside a settlement. `1.0` is vanilla. `0` disables in-town recovery. |
| Wilderness stamina recovery multiplier | **5×** | Multiplies the hourly recovery rate while travelling on the campaign map. `1.0` is vanilla. `0` disables wilderness recovery. |
| Weapon tier prefixes | **On** | Player-crafted weapons display their quality word (Rusty / Dull / Balanced / Masterwork / Legendary) in inventory, the same way pre-tiered loot weapons do. Common-quality items are not renamed. |
| Use own prefixes only | **Off** | When on, this mod's prefix words are always used. When off, the game's own modifier name is preferred and the mod's prefix is only used as a fallback when the game would render the bare item name. |

All values are read at game launch from a plain XML file and clamped to safe ranges.

## How it works

Uses [Harmony](https://github.com/pardeike/Harmony) runtime patching. Patches are applied individually so that one missing target method cannot disable the others; failures are surfaced as red in-game messages.

- `DefaultSmithingModel.GetEnergyCostForRefining` — prefix that returns `0` when *Unlimited Crafting Stamina* is on.
- `DefaultSmithingModel.GetEnergyCostForSmithing` — prefix that returns `0` when *Unlimited Crafting Stamina* is on.
- `DefaultSmithingModel.GetEnergyCostForSmelting` — prefix that returns `0` when *Unlimited Crafting Stamina* is on.
- `CraftingCampaignBehavior.GetStaminaHourlyRecoveryRate` — postfix that multiplies the original recovery rate by the configured in-town or wilderness multiplier (selected based on whether the hero's party currently sits in a settlement).
- `EquipmentElement.GetModifiedItemName` — postfix that prepends a tier word to the displayed name of player-crafted weapons that have an applied modifier.

No game files are modified. Patches are applied on load and reverted on unload.

**Mod conflicts:** Any other mod patching `DefaultSmithingModel`, `CraftingCampaignBehavior.GetStaminaHourlyRecoveryRate`, or `EquipmentElement.GetModifiedItemName` (including the original Better Smithing Continued from Nexus) may conflict — pick one.

## Requirements

- Mount & Blade II: Bannerlord (tested with current stable)
- .NET Framework 4.7.2

## Install / Use

### Players

1. Build & deploy with `./deploy.ps1`, or copy a release zip to `<Bannerlord>\Modules\BetterSmithingContinued\` so that the folder contains:
   - `SubModule.xml`
   - `BetterSmithingContinued.settings.xml` *(optional — defaults are used if missing)*
   - `bin\Win64_Shipping_Client\BetterSmithingContinued.dll`
   - `bin\Win64_Shipping_Client\0Harmony.dll`
2. Launch Bannerlord, open the **Mods** screen in the launcher, and enable **Better Smithing Continued**.
3. Start or load a campaign. You should see a green message at the top-left of the screen on the campaign map:
   `Better Smithing Continued: Loaded. Unlimited stamina ON. Recovery 5x in towns, 5x outside.`
4. Visit any town's smithy, open the Refine / Smelt / Forge tabs, and confirm the **Stamina** bar of the active hero does **not** decrease as you perform actions.

### Adjusting settings

Open `Modules\BetterSmithingContinued\BetterSmithingContinued.settings.xml` in any text editor:

```xml
<BetterSmithingContinuedSettings>
  <UnlimitedCraftingStamina>true</UnlimitedCraftingStamina>
  <StaminaRecoveryMultiplierInTowns>5.0</StaminaRecoveryMultiplierInTowns>
  <StaminaRecoveryMultiplierOutsideTowns>5.0</StaminaRecoveryMultiplierOutsideTowns>
  <AddWeaponTierPrefixes>true</AddWeaponTierPrefixes>
  <UseOwnPrefixesOnly>false</UseOwnPrefixesOnly>
</BetterSmithingContinuedSettings>
```

- Change `UnlimitedCraftingStamina` to `false` to keep vanilla stamina costs but still benefit from the recovery multipliers.
- Set the multipliers to `1.0` for vanilla rates, or `0` to disable that recovery channel entirely.
- Toggle `AddWeaponTierPrefixes` off if you prefer the vanilla unprefixed crafted-weapon names.
- Save the file and re-launch the game (settings are read once at startup).

Invalid or unparseable values silently fall back to the defaults — the mod will not crash on a malformed file.

## How to confirm it is working in-game

A quick verification checklist after enabling the mod:

1. **Load message** — When the campaign starts (or the main menu finishes loading), the green message
   `Better Smithing Continued: Loaded. Unlimited stamina ON. Recovery 5x in towns, 5x outside.`
   appears in the message log (top-left). If it isn't there, the module either didn't enable or another mod failed earlier.
2. **Zero stamina cost** — Visit any settlement with a smithy. Open **Refine** / **Smelt** / **Forge**, watch a hero's stamina bar, and perform an action. With `UnlimitedCraftingStamina=true` the bar must stay at the same value (the cost is zeroed before the bar is decremented).
3. **Recovery multiplier** — Move the campaign clock forward by one hour (Space, or rest). Note the hero's stamina increase. With the default 5× multiplier the per-hour gain should be roughly **five times** vanilla. Toggle `UnlimitedCraftingStamina` off and set the multiplier to `1.0` to see vanilla recovery for comparison.
4. **Toggle off** — Set `UnlimitedCraftingStamina=false`, restart the game, and confirm that performing a refine/smelt/forge action again drains stamina normally. This proves the patch is genuinely respecting the setting (rather than the bar simply being broken).
5. **Tier prefixes** — Forge a weapon. Open the inventory and look at the new item: with `AddWeaponTierPrefixes=true` it should be listed as e.g. *Masterwork Long Sword* / *Balanced Two Handed Sword*. Common-quality crafts keep their bare name. Toggle the setting off and re-craft to compare.

## Build & test

```powershell
# Build
dotnet build src\BetterSmithingContinued\BetterSmithingContinued.csproj -c Release

# Run all tests
dotnet test tests\BetterSmithingContinued.Tests\BetterSmithingContinued.Tests.csproj

# Build & deploy to game
./deploy.ps1
```

`GameFolder` defaults to `C:\Games\steamapps\common\Mount & Blade II Bannerlord`. Pass `-GameFolder` to `deploy.ps1` (or override the `<GameFolder>` MSBuild property) if your install lives elsewhere.

## Project layout

```
BetterSmithingContinued/
├── BetterSmithingContinued.sln
├── deploy.ps1
├── Module/
│   ├── SubModule.xml
│   └── BetterSmithingContinued.settings.xml
├── src/BetterSmithingContinued/
│   ├── BetterSmithingContinued.csproj
│   ├── BetterSmithingSettings.cs
│   ├── SubModule.cs
│   └── Patches/
│       ├── StaminaRecoveryPatch.cs
│       ├── UnlimitedStaminaPatches.cs
│       └── WeaponTierPrefixPatch.cs
└── tests/BetterSmithingContinued.Tests/
    ├── BetterSmithingContinued.Tests.csproj
    ├── BetterSmithingSettingsTests.cs
    ├── StaminaRecoveryPatchTests.cs
    ├── SubModuleTests.cs
    ├── UnlimitedStaminaPatchTests.cs
    └── WeaponTierPrefixPatchTests.cs
```

## Credits

- Inspired by **Better Smithing Continued** on Nexus Mods.
- Uses [Harmony](https://github.com/pardeike/Harmony) by Andreas Pardeike.
