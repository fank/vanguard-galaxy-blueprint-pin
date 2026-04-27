using Behaviour.Crafting;
using Behaviour.UI.Forge;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using VGBlueprintPin.State;
using VGBlueprintPin.UI;
using VGBlueprintPin.Util;

namespace VGBlueprintPin.Patches;

// Caches ForgeIngredientRow prefab + injects the pin button when the Forge UI
// spawns. The Forge UI is a fresh instance each time the player opens the
// Forge tab, so we re-hook every time.
[HarmonyPatch(typeof(ForgeUI), "Awake")]
internal static class ForgeUI_Awake_Patch
{
    private static void Postfix(ForgeUI __instance)
    {
        try
        {
            // ForgeUI.tabContents is a private SerializeField. The publicized
            // stub lets it compile, but Mono enforces accessibility at runtime,
            // so reach it via Traverse (same approach VGHangar uses).
            var tabContents = ForgeReflection.GetTabContents(__instance);
            if (tabContents == null) return;

            ForgePrefabCache.CaptureFrom(__instance, tabContents);
            PinnedBlueprintWidget.EnsureSpawned();
            PinButton.EnsureExists(__instance, tabContents);
        }
        catch (System.Exception e)
        {
            Plugin.Log.LogError($"[VGBlueprintPin] ForgeUI.Awake postfix failed: {e}");
        }
    }
}

// Sync the pin button label whenever the user clicks a different recipe.
[HarmonyPatch(typeof(ForgeTabContents), nameof(ForgeTabContents.SetSelectedRecipe))]
internal static class ForgeTabContents_SetSelectedRecipe_Patch
{
    private static void Postfix(ForgeTabContents __instance,
        CraftingRecipe parentRecipe, CraftingRecipe subRecipe, List<CraftingRecipe> availableSubRecipes)
    {
        try
        {
            // PinButton lives on the same ForgeTabContents — refresh if it exists.
            if (PinButton.Instance != null) PinButton.Instance.Refresh();
        }
        catch (System.Exception e)
        {
            Plugin.Log.LogError($"[VGBlueprintPin] SetSelectedRecipe postfix failed: {e}");
        }
    }
}
