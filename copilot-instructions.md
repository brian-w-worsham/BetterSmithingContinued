# Copilot Instructions — Better Smithing Continued

This project is a Bannerlord singleplayer Harmony mod targeting .NET Framework 4.7.2.

## Goals

- Re-create the gameplay tuning portion of the Nexus mod
  [Better Smithing Continued](https://www.nexusmods.com/mountandblade2bannerlord/mods/4318)
  using only Harmony and the vanilla Bannerlord assemblies — no `ButterLib`,
  `MCM`, or custom Gauntlet UI dependencies.
- Provide an XML-based settings file so users can tune the mod without rebuilding.
- Keep the implementation small, well-tested, and aligned with the conventions
  used by sibling mods in this workspace (`SmithForever`, `TrueTownGold`, `RecruitRefresher`, etc.).

## Original mod reference (read-only)

A copy of the original mod's deployed files lives at
`downloaded_mod/BetterSmithingContinued/`. Treat it as authoritative when you
need to confirm:

- TaleWorlds method names and signatures the original patches target.
- The full feature list and default values (see
  `downloaded_mod/BetterSmithingContinued/ModuleData/Languages/EN/bsc_strings.xml`
  for the original setting names — `BSC_SPN_41`/`BSC_SPN_42`/`BSC_SPN_43` map to
  our three settings).
- The original `SubModule.xml` declares dependencies on `Bannerlord.Harmony`,
  `Bannerlord.ButterLib`, and `Bannerlord.MBOptionScreen`. Our re-implementation
  intentionally drops those — do **not** add them back without a discussion.

The `downloaded_mod/` folder is `.gitignore`d. It is reference material only;
never copy code or binaries from it into the shipped module.

## Project conventions

- Source in `src/BetterSmithingContinued/`.
- Tests in `tests/BetterSmithingContinued.Tests/`.
- Patch classes in `src/BetterSmithingContinued/Patches/`.
- Use `internal` for everything except `SubModule`. Tests see internals via
  `InternalsVisibleTo("BetterSmithingContinued.Tests")` declared in the csproj.
- Settings are XML, loaded by `BetterSmithingSettings.LoadFromDefaultPath()`.
  Always parse with `CultureInfo.InvariantCulture` and fall back to defaults on
  any parsing/IO error.
- Favor small pure helper methods (e.g. `ApplyMultiplier`, `ResolveMultiplier`,
  `ValidateMultiplier`) for logic that should be unit-tested without
  constructing in-game objects.

## Patch safety rules

- Patch only `TaleWorlds.CampaignSystem.GameComponents.DefaultSmithingModel`.
- Stamina-cost prefixes must check `BetterSmithingSettings.Current.UnlimitedCraftingStamina`
  and return `true` (run original) when disabled. Do not unconditionally zero
  the result.
- The recovery `Postfix` must scale the result, never replace it. Negative or
  non-finite multipliers must leave the original untouched.
- Do not patch UI / Gauntlet / hotkey / saved-designs code paths from the
  original mod — they are explicitly out of scope for this re-implementation.
- Do not add save-data format changes or new campaign behaviors.

## Settings rules

- Three settings only: `UnlimitedCraftingStamina` (bool, default `true`),
  `StaminaRecoveryMultiplierInTowns` (float ≥ 0, default `5.0`),
  `StaminaRecoveryMultiplierOutsideTowns` (float ≥ 0, default `5.0`).
- `0` is a legitimate value for the multipliers (disables that recovery channel).
- Validation rejects `NaN`, `±Infinity`, and negatives, falling back to defaults.

## Build and test

- Build:  `dotnet build src/BetterSmithingContinued/BetterSmithingContinued.csproj -c Release`
- Test:   `dotnet test tests/BetterSmithingContinued.Tests/BetterSmithingContinued.Tests.csproj`
- Deploy: `./deploy.ps1`

## Deploy expectations

- Target: `<Bannerlord>/Modules/BetterSmithingContinued/`
- Required outputs:
  - `SubModule.xml`
  - `BetterSmithingContinued.settings.xml` (preserved if user already edited it;
    new keys merged in by `deploy.ps1`)
  - `bin/Win64_Shipping_Client/BetterSmithingContinued.dll`
  - `bin/Win64_Shipping_Client/0Harmony.dll`

## Testing strategy

- `BetterSmithingSettingsTests` — defaults, XML parsing, fallback paths,
  invariant-culture decimal parsing, validation.
- `UnlimitedStaminaPatchTests` — each prefix toggles correctly with the
  `UnlimitedCraftingStamina` setting and skips/runs the original method
  appropriately. Patch attribute targeting is verified via reflection.
- `StaminaRecoveryPatchTests` — pure logic for `ApplyMultiplier`,
  `ResolveMultiplier`, and `IsHeroInSettlement` (null-hero path). Postfix
  signature verified via reflection.
- `SubModuleTests` — entry-point structural invariants and assembly-level
  checks (patch count, declaring type, `InternalsVisibleTo`, Harmony id).
- Aim for >80% coverage on `BetterSmithingSettings` and the patch helper
  methods. Game-object-bound code paths are intentionally exercised only by
  in-game manual verification (see README).

## Code style

- C# 9.0 features where useful.
- XML documentation on all public/internal types and methods.
- Small, single-purpose methods. No silent exception swallowing inside hot
  paths; use try/catch only at the `OnSubModuleLoad` boundary and at the
  settings IO boundary.

## Known limitations

- Patch target method names depend on `DefaultSmithingModel` internals; if
  TaleWorlds renames `GetEnergyCostFor*` or `GetSmithingStaminaIncreasePerHour`,
  the patches must be updated.
- The mod replicates only the stamina-related portion of the original Nexus
  mod. UI, hotkeys, saved weapon designs, smelt-all, and character-cycle
  features are not implemented and are out of scope.
