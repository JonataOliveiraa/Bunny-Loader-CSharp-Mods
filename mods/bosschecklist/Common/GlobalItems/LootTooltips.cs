using System.Collections.Generic;
using BossChecklistMod.Common.Players;
using BossChecklistMod.Common.UI;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using static BossChecklistMod.Common.ModText;

namespace BossChecklistMod.Common.GlobalItems;

public class LootTooltips : GlobalItem
{
    static string Hex(Color c) => c.R.ToString("x2") + c.G.ToString("x2") + c.B.ToString("x2");

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        BossLogUI? log = Refs.log;
        if (log == null || !log.BossLogVisible || log.PageNum < 0 || log.SelectedSubPage != LogPages.SUB_LOOT)
            return;
        BossChecklistPlayer? me = State.Me;
        if (me == null)
            return;
        int type = item.type;
        int index = tooltips.FindIndex(t => t.Mod == "Terraria" && t.Name == "JourneyResearch");
        string green = Hex(Main.teamColor[4]);
        if (me.obtained.Contains(type))
        {
            var line = new TooltipLine(Mod, "BossLog_Obtained", "[c/" + green + ":" + L("Log.LootAndCollection.Obtained") + "]");
            if (index >= 0)
                tooltips.Insert(index, line);
            else
                tooltips.Add(line);
        }
        if (index >= 0 || Util.Local.difficulty != 3)
            return;
        if (!GameBindings.TryGetSacrificeNumbers(Util.Local, type, out int have, out int need))
            return;
        bool done = have >= need;
        string text = done ? L("Log.LootAndCollection.Researched") : Vanilla("CommonItemTooltip.CreativeSacrificeNeeded", need - have);
        if (text.Length > 0)
            tooltips.Add(new TooltipLine(Mod, "BossLog_Researched", "[c/" + (done ? green : "FF78BB") + ":" + text + "]"));
    }
}
