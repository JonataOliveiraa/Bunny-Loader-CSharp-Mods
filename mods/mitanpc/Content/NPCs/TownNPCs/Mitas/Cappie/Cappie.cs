using mitanpc.Common;
using mitanpc.Content.Items.Armor.Vanity;
using Terraria.ModLoader;

namespace mitanpc.Content.NPCs.TownNPCs.Mitas.Cappie;

[AutoloadHead]
public class Cappie : TownNPCBase
{
    public override void AddShops()
    {
        new NPCShop(Type, "Shop").Add<MitasCap>().Register();
    }
}
