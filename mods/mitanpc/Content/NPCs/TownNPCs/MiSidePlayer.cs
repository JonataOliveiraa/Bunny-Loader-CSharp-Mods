using mitanpc.Common;
using mitanpc.Content.Items.Pets;
using mitanpc.Content.Items.Potions;
using Terraria.ID;
using Terraria.ModLoader;

namespace mitanpc.Content.NPCs.TownNPCs;

[AutoloadHead]
public class MiSidePlayer : TownNPCBase
{
    public override void SetDefaults()
    {
        base.SetDefaults();
        NPC.width = 18;
        NPC.height = 40;
        AnimationType = NPCID.Guide;
    }

    public override void AddShops()
    {
        new NPCShop(Type, "Shop")
            .Add<GameConsole>()
            .Add<Ramen>()
            .Add<AlarmClock>()
            .Register();
    }
}
