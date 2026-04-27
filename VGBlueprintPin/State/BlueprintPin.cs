using System;
using Behaviour.Crafting;

namespace VGBlueprintPin.State;

// Single pinned blueprint, in-memory only. Designed so a future multi-pin
// extension can swap this for a list without changing call sites.
internal static class BlueprintPin
{
    public static CraftingRecipe? Current { get; private set; }
    public static int Count { get; private set; } = 1;

    public static event Action? Changed;

    public static void Set(CraftingRecipe? recipe, int count = 1)
    {
        int clamped = count < 1 ? 1 : count;
        if (Current == recipe && Count == clamped) return;
        Current = recipe;
        Count = clamped;
        Changed?.Invoke();
    }

    public static void Clear() => Set(null, 1);

    // Reduce the pinned count after the player crafts some of the recipe.
    // If the pin would drop to zero, the pin is cleared entirely.
    public static void DecrementBy(int amount)
    {
        if (Current == null || amount <= 0) return;
        int next = Count - amount;
        if (next <= 0)
        {
            Clear();
            return;
        }
        Count = next;
        Changed?.Invoke();
    }

    // Unity uses overloaded equality where a destroyed MonoBehaviour compares
    // equal to null. Call this from a periodic tick to drop a pin whose
    // CraftingRecipe was unloaded by a scene change.
    public static void DropIfDestroyed()
    {
        if (Current != null && (UnityEngine.Object)Current == null)
        {
            Current = null;
            Changed?.Invoke();
        }
    }
}
