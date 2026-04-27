using Behaviour.UI.Forge;
using TMPro;
using UnityEngine;
using VGBlueprintPin.Util;

namespace VGBlueprintPin.State;

// References to the Forge UI prefabs we reuse for the pinned widget. Populated
// the first time ForgeUI.Awake runs in a session, which is guaranteed before
// any pin is set (you can only pin from inside the Forge).
internal static class ForgePrefabCache
{
    public static ForgeIngredientRow? IngredientRowPrefab { get; private set; }

    // A TMP font asset captured from one of the Forge's labels. Used for the
    // pinned widget's header text so it visually matches the rest of the UI.
    public static TMP_FontAsset? Font { get; private set; }

    public static bool IsReady => IngredientRowPrefab != null;

    public static void CaptureFrom(ForgeUI forgeUI, ForgeTabContents tabContents)
    {
        // Private SerializeField — go through Traverse (see ForgeReflection).
        if (IngredientRowPrefab == null)
            IngredientRowPrefab = ForgeReflection.GetIngredientPrefab(tabContents);

        if (Font == null)
        {
            var anyText = forgeUI.GetComponentInChildren<TMP_Text>(includeInactive: true);
            if (anyText != null) Font = anyText.font;
        }
    }
}
