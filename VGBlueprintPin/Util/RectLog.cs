using System.Text;
using UnityEngine;
using BepInEx.Logging;

namespace VGBlueprintPin.Util;

internal static class RectLog
{
    // Recursive hierarchy dump: name + active + world rect of every child,
    // up to a depth limit. Lets us see exactly what occupies which screen area.
    public static void DumpTree(ManualLogSource log, string label, Transform root, int maxDepth = 5)
    {
        log.LogInfo($"[VGBlueprintPin][tree] === {label} ===");
        DumpTreeRec(log, root, 0, maxDepth);
        log.LogInfo("[VGBlueprintPin][tree] === end ===");
    }

    private static void DumpTreeRec(ManualLogSource log, Transform t, int depth, int maxDepth)
    {
        var indent = new string(' ', depth * 2);
        var sb = new StringBuilder();
        sb.Append("[VGBlueprintPin][tree] ").Append(indent).Append(t.name);
        sb.Append(" active=").Append(t.gameObject.activeInHierarchy);
        if (t is RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            sb.Append(" world=[").Append(corners[0]).Append("→").Append(corners[2]).Append("]");
            sb.Append(" size=").Append(rt.rect.size);
        }
        log.LogInfo(sb.ToString());
        if (depth >= maxDepth) return;
        for (int i = 0; i < t.childCount; i++)
        {
            DumpTreeRec(log, t.GetChild(i), depth + 1, maxDepth);
        }
    }

    // Compact dump of a RectTransform: anchored position, size, anchors, pivot,
    // world rect (in screen pixels for an overlay canvas), active state, and
    // parent chain up to the canvas. Lets us see at a glance whether something
    // ended up off-screen, zero-sized, or under an inactive parent.
    public static string Dump(string label, RectTransform? rt)
    {
        if (rt == null) return $"{label}=null";
        var sb = new StringBuilder();
        sb.Append(label).Append("={");
        sb.Append("name=").Append(rt.name);
        sb.Append(" active=").Append(rt.gameObject.activeInHierarchy);
        sb.Append(" pos=").Append(rt.anchoredPosition);
        sb.Append(" size=").Append(rt.sizeDelta);
        sb.Append(" anchorMin=").Append(rt.anchorMin);
        sb.Append(" anchorMax=").Append(rt.anchorMax);
        sb.Append(" pivot=").Append(rt.pivot);

        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        sb.Append(" worldRect=[bl=").Append(corners[0]).Append(" tr=").Append(corners[2]).Append("]");

        sb.Append(" parents=[");
        var p = rt.parent;
        int depth = 0;
        while (p != null && depth < 6)
        {
            if (depth > 0) sb.Append(" > ");
            sb.Append(p.name);
            if (p is RectTransform prt)
            {
                sb.Append("(active=").Append(prt.gameObject.activeInHierarchy);
                sb.Append(",size=").Append(prt.rect.size).Append(")");
            }
            p = p.parent;
            depth++;
        }
        sb.Append("]");

        sb.Append("}");
        return sb.ToString();
    }
}
