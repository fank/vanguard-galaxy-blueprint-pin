using Behaviour.Crafting;
using HarmonyLib;
using Source.Mining;
using VGBlueprintPin.State;

namespace VGBlueprintPin.Patches;

// When the player kicks off a forge job, decrement the pinned count by how
// many they queued. Pin clears automatically when count reaches zero.
//
// We hook StartJob (not TryStartJob) — TryStartJob short-circuits on full
// queue, while StartJob is what actually deducts ingredients/credits. A true
// return means the job is queued and the pin should reflect that.
[HarmonyPatch(typeof(Forge), nameof(Forge.StartJob))]
internal static class Forge_StartJob_Patch
{
    private static void Postfix(CraftingRecipe recipe, int amount, bool __result)
    {
        if (!__result) return;
        if (recipe == null || BlueprintPin.Current != recipe) return;

        BlueprintPin.DecrementBy(amount);
        Plugin.Log.LogInfo($"[VGBlueprintPin] crafted {amount}x {recipe.displayName} → pin remaining={BlueprintPin.Count} (cleared={BlueprintPin.Current == null})");
    }
}
