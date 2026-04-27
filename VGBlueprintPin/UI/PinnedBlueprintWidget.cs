using System.Collections.Generic;
using Behaviour.Crafting;
using Behaviour.UI.Forge;
using Behaviour.UI.Side_Menu.SideTabs;
using Behaviour.UI.Tooltip;
using Source.Galaxy.POI;
using Source.Util;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VGBlueprintPin.State;

namespace VGBlueprintPin.UI;

// Persistent HUD widget rendering the currently pinned blueprint. Lives on its
// own screen-space-overlay Canvas so it survives scene transitions; visibility
// is gated each frame against the same conditions the cargo indicator uses.
internal class PinnedBlueprintWidget : MonoBehaviour
{
    private static PinnedBlueprintWidget? _instance;

    private RectTransform _card = null!;
    private Image _headerIcon = null!;
    private TMP_Text _headerLabel = null!;
    private RectTransform _rowsParent = null!;
    private Button _closeButton = null!;

    private readonly List<ForgeIngredientRow> _rows = new();
    private float _refreshTimer;
    private float _findIndicatorTimer;
    private CraftingRecipe? _renderedFor;
    private CargoIndicator? _cargoIndicator;
    // Track the previous value of each gate so we can log when *any* of them
    // flip — not just when the final visibility flips.
    private bool? _prevHasPin;
    private bool? _prevCargoVisible;
    private bool? _prevForgeOpen;

    public static void EnsureSpawned()
    {
        if (_instance != null)
        {
            Plugin.Log.LogInfo("[VGBlueprintPin] Widget.EnsureSpawned: already spawned");
            return;
        }
        Plugin.Log.LogInfo("[VGBlueprintPin] Widget.EnsureSpawned: spawning canvas + card");

        var canvasGo = new GameObject("VGBlueprintPin.WidgetCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        UnityEngine.Object.DontDestroyOnLoad(canvasGo);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // 100 puts us above the main station UI panels but below the
        // game's tooltip canvas, which sits at a much higher order.
        canvas.sortingOrder = 100;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var widget = canvasGo.AddComponent<PinnedBlueprintWidget>();
        widget.Build(canvasGo.transform);
        _instance = widget;

        BlueprintPin.Changed += widget.OnPinChanged;
        widget.OnPinChanged();

        Canvas.ForceUpdateCanvases();
        Plugin.Log.LogInfo("[VGBlueprintPin] " + Util.RectLog.Dump("widgetCanvas", canvasGo.GetComponent<RectTransform>()));
        Plugin.Log.LogInfo("[VGBlueprintPin] " + Util.RectLog.Dump("widgetCard", widget._card));
    }

    private void Build(Transform parent)
    {
        // Outer card — anchored bottom-right of the screen, pivot at
        // bottom-right, so a positive y offset lifts it just above the side
        // menu / cargo indicator (which lives in the bottom-right ~324 px).
        var cardGo = new GameObject("Card",
            typeof(RectTransform), typeof(Image));
        var cardRt = (RectTransform)cardGo.transform;
        cardRt.SetParent(parent, worldPositionStays: false);
        cardRt.anchorMin = new Vector2(1f, 0f);
        cardRt.anchorMax = new Vector2(1f, 0f);
        cardRt.pivot = new Vector2(1f, 0f);
        cardRt.sizeDelta = new Vector2(300f, 200f);
        cardRt.anchoredPosition = new Vector2(-12f, 340f);
        var cardBg = cardGo.GetComponent<Image>();
        cardBg.color = new Color(0.06f, 0.08f, 0.12f, 0.88f);
        _card = cardRt;

        // Header row: icon + name + close button.
        var iconGo = new GameObject("HeaderIcon", typeof(RectTransform), typeof(Image));
        var iconRt = (RectTransform)iconGo.transform;
        iconRt.SetParent(cardRt, worldPositionStays: false);
        iconRt.anchorMin = new Vector2(0f, 1f);
        iconRt.anchorMax = new Vector2(0f, 1f);
        iconRt.pivot = new Vector2(0f, 1f);
        iconRt.sizeDelta = new Vector2(28f, 28f);
        iconRt.anchoredPosition = new Vector2(8f, -8f);
        _headerIcon = iconGo.GetComponent<Image>();
        _headerIcon.preserveAspect = true;

        var labelGo = new GameObject("HeaderLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        var labelRt = (RectTransform)labelGo.transform;
        labelRt.SetParent(cardRt, worldPositionStays: false);
        labelRt.anchorMin = new Vector2(0f, 1f);
        labelRt.anchorMax = new Vector2(1f, 1f);
        labelRt.pivot = new Vector2(0f, 1f);
        labelRt.offsetMin = new Vector2(44f, -36f);
        labelRt.offsetMax = new Vector2(-32f, -8f);
        _headerLabel = labelGo.GetComponent<TextMeshProUGUI>();
        _headerLabel.alignment = TextAlignmentOptions.MidlineLeft;
        _headerLabel.fontSize = 14f;
        _headerLabel.fontStyle = FontStyles.Bold;
        if (ForgePrefabCache.Font != null) _headerLabel.font = ForgePrefabCache.Font;

        var closeGo = new GameObject("Close",
            typeof(RectTransform), typeof(Image), typeof(Button));
        var closeRt = (RectTransform)closeGo.transform;
        closeRt.SetParent(cardRt, worldPositionStays: false);
        closeRt.anchorMin = new Vector2(1f, 1f);
        closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot = new Vector2(1f, 1f);
        closeRt.sizeDelta = new Vector2(20f, 20f);
        closeRt.anchoredPosition = new Vector2(-6f, -6f);
        closeGo.GetComponent<Image>().color = new Color(0.4f, 0.15f, 0.15f, 0.9f);
        var closeLabelGo = new GameObject("X", typeof(RectTransform), typeof(TextMeshProUGUI));
        var closeLabelRt = (RectTransform)closeLabelGo.transform;
        closeLabelRt.SetParent(closeRt, worldPositionStays: false);
        closeLabelRt.anchorMin = Vector2.zero;
        closeLabelRt.anchorMax = Vector2.one;
        closeLabelRt.offsetMin = Vector2.zero;
        closeLabelRt.offsetMax = Vector2.zero;
        var closeLabel = closeLabelGo.GetComponent<TextMeshProUGUI>();
        closeLabel.text = "X";
        closeLabel.alignment = TextAlignmentOptions.Center;
        closeLabel.fontSize = 13f;
        if (ForgePrefabCache.Font != null) closeLabel.font = ForgePrefabCache.Font;
        _closeButton = closeGo.GetComponent<Button>();
        _closeButton.onClick.AddListener(BlueprintPin.Clear);

        // Ingredient rows live below the header.
        var rowsGo = new GameObject("Rows", typeof(RectTransform));
        var rowsRt = (RectTransform)rowsGo.transform;
        rowsRt.SetParent(cardRt, worldPositionStays: false);
        rowsRt.anchorMin = new Vector2(0f, 1f);
        rowsRt.anchorMax = new Vector2(1f, 1f);
        rowsRt.pivot = new Vector2(0f, 1f);
        rowsRt.offsetMin = new Vector2(8f, -200f);
        rowsRt.offsetMax = new Vector2(-8f, -42f);
        _rowsParent = rowsRt;
    }

    private void OnPinChanged()
    {
        Plugin.Log.LogInfo($"[VGBlueprintPin] OnPinChanged: pin={(BlueprintPin.Current != null ? BlueprintPin.Current.displayName : "null")}");
        Rebuild();
    }

    private void Rebuild()
    {
        // Clear existing rows.
        foreach (var row in _rows)
        {
            if (row != null) UnityEngine.Object.Destroy(row.gameObject);
        }
        _rows.Clear();
        _renderedFor = BlueprintPin.Current;

        var recipe = BlueprintPin.Current;
        if (recipe == null || (UnityEngine.Object)recipe == null) return;
        if (!ForgePrefabCache.IsReady) return;

        _headerIcon.sprite = recipe.icon;
        // recipe.displayName is a localization key (e.g. "@DepTauntPylonName")
        // — run it through the game's Translation helper before showing.
        // Prefix with the count so it reads "5x Taunt Pylon".
        var translated = Translation.Translate(recipe.displayName);
        _headerLabel.text = BlueprintPin.Count > 1
            ? $"{BlueprintPin.Count}x {translated}"
            : translated;

        int y = 0;
        foreach (var ing in recipe.GetIngredientMaterials())
        {
            var row = SpawnRow();
            row.InitializeMaterial(ing.material, ing.amount, isResult: false);
            ((RectTransform)row.transform).anchoredPosition = new Vector2(0f, y);
            y -= 24;
        }
        foreach (var ing in recipe.GetIngredientItems())
        {
            var row = SpawnRow();
            row.InitializeItem(ing.item, ing.count, isResult: false);
            // Replace vanilla click handler with our station-aware one.
            row.enabled = false;
            var click = row.gameObject.AddComponent<PinnedRowClick>();
            click.Item = ing.item;
            ((RectTransform)row.transform).anchoredPosition = new Vector2(0f, y);
            y -= 24;
        }

        // Apply the count multiplier to all rows so they show
        // (ingredient.amount * count) vs have-count.
        foreach (var row in _rows)
        {
            try { row.UpdateAmount(BlueprintPin.Count); } catch { }
        }

        // Resize card to fit rows.
        float cardHeight = 42f + (-y) + 12f;
        _card.sizeDelta = new Vector2(_card.sizeDelta.x, cardHeight);
        _rowsParent.offsetMin = new Vector2(8f, -(cardHeight - 6f));
    }

    private ForgeIngredientRow SpawnRow()
    {
        var row = UnityEngine.Object.Instantiate(ForgePrefabCache.IngredientRowPrefab!, _rowsParent);
        // ItemTooltipSource on the cloned row is left enabled — the row's
        // own InitializeItem/Material wires up SetItem, so hovering shows
        // the same tooltip the forge does.
        _rows.Add(row);
        return row;
    }

    private void Update()
    {
        // The cargo indicator's own active-in-hierarchy is the truthful "HUD is
        // showing right now" signal: it's parented under the side menu, so it
        // hides whenever the map / pause / station tabs / cutscenes hide that
        // panel. Mirroring it gives us the visibility behaviour we agreed on
        // for free, without having to enumerate every full-screen UI.
        if (_cargoIndicator == null || (UnityEngine.Object)_cargoIndicator == null)
        {
            _findIndicatorTimer += Time.unscaledDeltaTime;
            if (_findIndicatorTimer >= 1f)
            {
                _findIndicatorTimer = 0f;
                _cargoIndicator = UnityEngine.Object.FindAnyObjectByType<CargoIndicator>(FindObjectsInactive.Include);
                if (_cargoIndicator != null)
                {
                    Plugin.Log.LogInfo("[VGBlueprintPin] " + Util.RectLog.Dump("cargoIndicator", _cargoIndicator.transform as RectTransform));
                }
                else
                {
                    Plugin.Log.LogInfo("[VGBlueprintPin] CargoIndicator not yet found");
                }
            }
        }

        bool hasPin = BlueprintPin.Current != null && (UnityEngine.Object)BlueprintPin.Current != null;
        bool cargoVisible = _cargoIndicator != null && _cargoIndicator.gameObject.activeInHierarchy;
        bool forgeOpen = ForgeUI.current != null && ForgeUI.current.isActiveAndEnabled;
        // Show whenever a pin is set and the side menu is up. Showing
        // alongside the open Forge is fine — gives immediate confirmation
        // that pinning worked, and the widget is small + right-aligned so
        // it doesn't fight the forge panel for attention.
        bool visible = hasPin && cargoVisible;

        // Log on any individual gate change, not just the final visible flip.
        // That way we see "forgeOpen flipped to false" even if the widget
        // stays hidden because another gate is still closed.
        if (_prevHasPin != hasPin || _prevCargoVisible != cargoVisible || _prevForgeOpen != forgeOpen)
        {
            Plugin.Log.LogInfo(
                $"[VGBlueprintPin] gates: hasPin={hasPin} cargoVisible={cargoVisible} forgeOpen={forgeOpen} → visible={visible} (cargoIndicator={(_cargoIndicator != null ? "found" : "null")})");
            _prevHasPin = hasPin;
            _prevCargoVisible = cargoVisible;
            _prevForgeOpen = forgeOpen;
        }

        if (_card.gameObject.activeSelf != visible)
        {
            _card.gameObject.SetActive(visible);
        }
        if (!visible) return;

        // Drop the pin if its CraftingRecipe was destroyed.
        BlueprintPin.DropIfDestroyed();
        if (BlueprintPin.Current != _renderedFor)
        {
            Rebuild();
            return;
        }

        // Periodic count refresh (~2 Hz). The cargo indicator polls at the same rate.
        _refreshTimer += Time.unscaledDeltaTime;
        if (_refreshTimer < 0.5f) return;
        _refreshTimer = 0f;

        bool atStation = SpaceStation.current != null;
        foreach (var row in _rows)
        {
            if (row == null) continue;
            try
            {
                // Material rows are safe always; item rows touch SpaceStation.current.
                if (row.item == null || atStation)
                {
                    row.UpdateAmount(BlueprintPin.Count);
                }
            }
            catch (System.Exception e)
            {
                // First failure logs; we don't want a spam loop.
                Plugin.Log.LogDebug($"[VGBlueprintPin] Row refresh skipped: {e.Message}");
            }
        }
    }
}
