using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace GnorpBuilderMod.Common;

public class GnorpTooltips : GlobalItem
{
    static string OnOff(bool value) => Util.T(value ? "BuildConfig.ValueOn" : "BuildConfig.ValueOff");

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        if (item.ModItem is not Globe) return;

        GnorpConfig config = GnorpConfig.Instance;
        tooltips.Add(new TooltipLine(Mod, "AnchorInfo", Util.T("BuildConfig.AnchorInfo", PreviewSystem.AnchorName())));
        tooltips.Add(new TooltipLine(Mod, "PreviewInfo",
            Util.T("BuildConfig.ImagePreviewInfo", OnOff(config.ImagePreview)) + "   " + Util.T("BuildConfig.DustPreviewInfo", OnOff(config.EnablePreviews))));
        tooltips.Add(new TooltipLine(Mod, "DoubleClickInfo", Util.T("BuildConfig.DoubleClickInfo", OnOff(config.RequireDoubleClickPlacement))));
        tooltips.Add(new TooltipLine(Mod, "SettingsHint", Util.T("BuildConfig.SettingsHint")));
    }
}
