using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace BetterSmithingContinued.Patches
{
    /// <summary>
    /// Postfixes <see cref="EquipmentElement"/>.<c>GetModifiedItemName</c> so
    /// that player-crafted weapons display a tier prefix
    /// (<c>Rusty</c>, <c>Dull</c>, <c>Balanced</c>, <c>Masterwork</c>,
    /// <c>Legendary</c>) the same way pre-tiered loot does.
    /// </summary>
    /// <remarks>
    /// Mirrors the corresponding feature in the original Better Smithing
    /// Continued mod. Only player-crafted items that have an applied
    /// <see cref="ItemModifier"/> are renamed; other items are left untouched.
    /// </remarks>
    [HarmonyPatch(typeof(EquipmentElement), "GetModifiedItemName")]
    internal static class WeaponTierPrefixPatch
    {
        /// <param name="__instance">The equipment element being named.</param>
        /// <param name="__result">The name produced by the original method; replaced in-place when applicable.</param>
        internal static void Postfix(EquipmentElement __instance, ref TextObject __result)
        {
            __result = TryApplyTierPrefix(__instance, __result, BetterSmithingSettings.Current);
        }

        /// <summary>
        /// Pure helper that performs the same decision the postfix performs,
        /// but without touching ref parameters so it can be unit-tested.
        /// Returns the new name to display, or <paramref name="originalResult"/>
        /// when no change should be made.
        /// </summary>
        internal static TextObject TryApplyTierPrefix(
            EquipmentElement element,
            TextObject originalResult,
            BetterSmithingSettings settings)
        {
            if (settings == null || !settings.AddWeaponTierPrefixes)
            {
                return originalResult;
            }

            ItemObject item = element.Item;
            ItemModifier modifier = element.ItemModifier;

            if (item == null || modifier == null)
            {
                return originalResult;
            }

            if (!item.IsCraftedByPlayer)
            {
                return originalResult;
            }

            return BuildPrefixedName(item, modifier, settings.UseOwnPrefixesOnly)
                   ?? originalResult;
        }

        /// <summary>
        /// Builds a tier-prefixed display name. Prefers the game's own
        /// modifier name unless <paramref name="useOwnPrefixesOnly"/> is true
        /// or the modifier name renders identically to the bare item name
        /// (which happens for some unrenamed crafted modifiers).
        /// </summary>
        internal static TextObject BuildPrefixedName(
            ItemObject item,
            ItemModifier modifier,
            bool useOwnPrefixesOnly)
        {
            if (item == null || modifier == null)
            {
                return null;
            }

            TextObject candidate = null;
            bool fallbackToOwnPrefix = useOwnPrefixesOnly;

            if (!fallbackToOwnPrefix)
            {
                TextObject modifierName = modifier.Name;
                if (modifierName != null)
                {
                    candidate = CopyAndBindItemName(modifierName, item.Name);
                    fallbackToOwnPrefix = string.Equals(
                        candidate.ToString().Trim(),
                        item.Name?.ToString()?.Trim(),
                        System.StringComparison.Ordinal);
                }
                else
                {
                    fallbackToOwnPrefix = true;
                }
            }

            if (fallbackToOwnPrefix)
            {
                TextObject template = GetTierTemplate(modifier.ItemQuality);
                if (template == null)
                {
                    return candidate; // no template for this quality; keep modifier-name candidate (may be null)
                }
                candidate = CopyAndBindItemName(template, item.Name);
            }

            return candidate;
        }

        /// <summary>
        /// Returns the prefix template for a given <see cref="ItemQuality"/>,
        /// using the same words as the original mod. Returns null for
        /// <see cref="ItemQuality.Common"/> (unmodified items keep their bare name).
        /// </summary>
        internal static TextObject GetTierTemplate(ItemQuality quality)
        {
            switch (quality)
            {
                case ItemQuality.Poor:
                    return new TextObject("Rusty {ITEMNAME}");
                case ItemQuality.Inferior:
                    return new TextObject("Dull {ITEMNAME}");
                case ItemQuality.Common:
                    return null;
                case ItemQuality.Fine:
                    return new TextObject("Balanced {ITEMNAME}");
                case ItemQuality.Masterwork:
                    return new TextObject("Masterwork {ITEMNAME}");
                case ItemQuality.Legendary:
                    return new TextObject("Legendary {ITEMNAME}");
                default:
                    return null;
            }
        }

        private static TextObject CopyAndBindItemName(TextObject template, TextObject itemName)
        {
            TextObject copy = template.CopyTextObject();
            copy.SetTextVariable("ITEMNAME", itemName ?? new TextObject(string.Empty));
            return copy;
        }
    }
}
