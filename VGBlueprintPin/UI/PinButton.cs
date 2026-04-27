using Behaviour.Crafting;
using Behaviour.UI.Forge;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VGBlueprintPin.State;
using VGBlueprintPin.Util;

namespace VGBlueprintPin.UI;

// The pin toggle injected into the Forge's selected-recipe panel. Lives as a
// child of the panel root and updates its label/colour whenever
// ForgeTabContents.SetSelectedRecipe runs.
internal class PinButton : MonoBehaviour
{
    private ForgeTabContents _tabContents = null!;
    private TMP_Text _label = null!;
    private Image _bg = null!;

    public static PinButton? Instance { get; private set; }

    public static void EnsureExists(ForgeUI forgeUI, ForgeTabContents tabContents)
    {
        if (Instance != null && Instance._tabContents == tabContents)
        {
            Plugin.Log.LogInfo($"[VGBlueprintPin] PinButton.EnsureExists: already exists for this tabContents (parent={(Instance.transform.parent != null ? Instance.transform.parent.name : "null")})");
            return;
        }

        // Parent on the RecipeDetails panel (the recipe Icon's parent) and
        // place the button in the empty band between the Results row
        // (y=140-172) and the Deposit-in-Cargo toggle (y=78-110). That 30 px
        // strip spans the full width of the panel and contains nothing —
        // confirmed by walking the live UI tree.
        var recipeIcon = ForgeReflection.GetRecipeIcon(tabContents);
        var anchor = recipeIcon != null && recipeIcon.transform.parent != null
            ? recipeIcon.transform.parent as RectTransform
            : null;
        Plugin.Log.LogInfo($"[VGBlueprintPin] PinButton.EnsureExists: anchor={(anchor != null ? anchor.name : "null")} forgeUI={forgeUI.name}");
        if (anchor == null)
        {
            Plugin.Log.LogWarning("[VGBlueprintPin] No anchor found for pin button — skipping injection.");
            return;
        }

        var go = new GameObject("VGBlueprintPin.PinButton",
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(PinButton));
        var rt = (RectTransform)go.transform;
        rt.SetParent(anchor, worldPositionStays: false);
        // Right-aligned, 116 px above the panel bottom — lands in the empty
        // gap above the Deposit-in-Cargo toggle.
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.sizeDelta = new Vector2(80f, 24f);
        rt.anchoredPosition = new Vector2(-8f, 114f);

        var bg = go.GetComponent<Image>();
        bg.color = new Color(0.10f, 0.12f, 0.16f, 0.85f);

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var labelRt = (RectTransform)labelGo.transform;
        labelRt.SetParent(rt, worldPositionStays: false);
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        var label = labelGo.GetComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 12f;
        label.fontStyle = FontStyles.Bold;
        if (ForgePrefabCache.Font != null) label.font = ForgePrefabCache.Font;

        var pin = go.GetComponent<PinButton>();
        pin._tabContents = tabContents;
        pin._label = label;
        pin._bg = bg;

        var btn = go.GetComponent<Button>();
        btn.onClick.AddListener(pin.OnClicked);

        Instance = pin;
        pin.Refresh();

        // Geometry dump — once at creation time. Force a layout rebuild first
        // so anchor-resolved sizes are real and not (0,0) defaults.
        Canvas.ForceUpdateCanvases();
        Plugin.Log.LogInfo("[VGBlueprintPin] " + Util.RectLog.Dump("pinButton", rt));
        Plugin.Log.LogInfo("[VGBlueprintPin] " + Util.RectLog.Dump("forgeUIRoot", anchor));
    }

    public void Refresh()
    {
        var current = _tabContents != null ? _tabContents.subRecipe : null;
        bool pinned = current != null && BlueprintPin.Current == current;
        _label.text = pinned ? "PINNED" : "PIN";
        _bg.color = pinned
            ? new Color(0.30f, 0.65f, 0.30f, 0.95f)
            : new Color(0.05f, 0.08f, 0.12f, 0.85f);
    }

    private void OnClicked()
    {
        var sub = _tabContents != null ? _tabContents.subRecipe : null;
        Plugin.Log.LogInfo($"[VGBlueprintPin] PinButton clicked. subRecipe={(sub != null ? sub.displayName : "null")} currentPin={(BlueprintPin.Current != null ? BlueprintPin.Current.displayName : "null")}");
        if (_tabContents == null || _tabContents.subRecipe == null) return;

        // Snapshot the count slider's current value so the widget shows
        // ingredients for "how many you want to craft", not just one.
        int count = 1;
        var slider = ForgeReflection.GetCountSlider(_tabContents);
        if (slider != null) count = Mathf.Max(1, Mathf.RoundToInt(slider.value));

        if (BlueprintPin.Current == _tabContents.subRecipe && BlueprintPin.Count == count)
            BlueprintPin.Clear();
        else
            BlueprintPin.Set(_tabContents.subRecipe, count);
        Refresh();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
