using System.Collections.Generic;
using Behaviour.Crafting;
using Behaviour.Item;
using Behaviour.UI.Forge;
using Behaviour.UI.Spacestation;
using Source.Galaxy.POI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VGBlueprintPin.UI;

// Replacement click handler for cloned ForgeIngredientRow instances. The vanilla
// row's OnPointerClick assumes ForgeUI.current exists and only handles items.
// We need to also work when the Forge is closed (open it via SpaceStationInterior)
// and to do nothing gracefully when the player isn't at a station.
internal class PinnedRowClick : MonoBehaviour, IPointerClickHandler
{
    public InventoryItemType? Item;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Item == null) return;

        CraftingRecipe? target = null;
        foreach (var recipe in CraftingRecipe.GetAvailable())
        {
            if (recipe.displayName == Item.displayName)
            {
                target = recipe;
                break;
            }
        }
        if (target == null) return;

        if (ForgeUI.current != null && ForgeUI.current.isActiveAndEnabled)
        {
            ForgeUI.current.SelectRecipe(target, new List<CraftingRecipe> { target });
            return;
        }

        // Forge closed. If we're inside the station UI we can switch tabs.
        if (SpaceStationInterior.instance != null && SpaceStation.current != null)
        {
            ForgeUI.preselectRecipe = target;
            SpaceStationInterior.instance.GoToLocation(SpaceStationFacility.Forge);
            return;
        }

        // Not at a station — silently ignore. The widget will have rendered the
        // row dim/non-interactive in this case.
    }
}
