using Behaviour.UI.Forge;
using HarmonyLib;
using UnityEngine;

namespace VGBlueprintPin.Util;

// Cached Traverse-based access to private SerializeFields on the Forge UI.
// The publicized stubs in lib/ make these compile-public, but the runtime
// Assembly-CSharp keeps them private and Mono throws FieldAccessException on
// direct reads. Going through Harmony's Traverse uses reflection under the
// hood, which Mono allows.
internal static class ForgeReflection
{
    public static ForgeTabContents? GetTabContents(ForgeUI ui)
        => Traverse.Create(ui).Field<ForgeTabContents>("tabContents").Value;

    public static ForgeIngredientRow? GetIngredientPrefab(ForgeTabContents tc)
        => Traverse.Create(tc).Field<ForgeIngredientRow>("ingredientPrefab").Value;

    public static RectTransform? GetIngredientsLabel(ForgeTabContents tc)
        => Traverse.Create(tc).Field<RectTransform>("ingredientsLabel").Value;

    public static UnityEngine.UI.Image? GetRecipeIcon(ForgeTabContents tc)
        => Traverse.Create(tc).Field<UnityEngine.UI.Image>("recipeIcon").Value;

    public static UnityEngine.UI.Slider? GetCountSlider(ForgeTabContents tc)
        => Traverse.Create(tc).Field<UnityEngine.UI.Slider>("countSlider").Value;
}
