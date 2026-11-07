using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;

namespace BossChecklistMod.Common;

public class MapIcons : ModMapLayer
{
    // Whether a type is a fragment, asked once: this runs for every dropped item, every frame.
    static readonly Dictionary<int, bool> fragments = new();
    static bool broken;

    static bool IsFragment(int type)
    {
        if (fragments.TryGetValue(type, out bool known))
            return known;
        bool fragment = Array.IndexOf(Data.FRAGMENTS, type) >= 0;
        if (!fragment)
        {
            RecipeGroup group = RecipeGroups.Fragment;
            fragment = !group.IsNull && group.Contains(type);
        }
        fragments[type] = fragment;
        return fragment;
    }

    static bool Whitelisted(int type)
    {
        FeatureConfiguration f = Cfg.Feature;
        if (ItemID.Sets.BossBag[type])
            return f.TreasureBagsOnMap;
        if (Array.IndexOf(Data.SCALES, type) >= 0)
            return f.ScalesOnMap;
        return IsFragment(type) && f.FragmentsOnMap;
    }

    public override void Draw(ref MapOverlayDrawContext context, ref string text)
    {
        if (broken)
            return;
        try
        {
            DrawItems(ref context);
        }
        catch (Exception e)
        {
            broken = true;
            Util.Log("Map icons disabled after an error: " + Util.Message(e));
        }
    }

    static void DrawItems(ref MapOverlayDrawContext context)
    {
        FeatureConfiguration f = Cfg.Feature;
        if (!f.TreasureBagsOnMap && !f.FragmentsOnMap && !f.ScalesOnMap)
            return;
        int n = Main.item.Length;
        for (int i = 0; i < n; i++)
        {
            WorldItem w = Main.item[i];
            if (w.IsNull || !w.active)
                continue;
            int type = w.type;
            if (!Whitelisted(type))
                continue;
            Texture2D texture = Tracker.ItemTexture(type);
            if (texture.IsNull)
                continue;
            Vector2 center = w.Center;
            MapOverlayDrawContext.DrawResult result = context.Draw(texture, new Vector2(center.X / 16f, center.Y / 16f), Alignment.Center);
            if (!result.IsMouseOver)
                continue;
            Item inner = w.inner;
            string? name = inner.HoverName;
            if (string.IsNullOrEmpty(name))
                name = inner.Name;
            if (!string.IsNullOrEmpty(name))
                Main.instance.MouseText(name, inner.rare, 0, -1, -1, -1, -1, 0);
        }
    }
}
