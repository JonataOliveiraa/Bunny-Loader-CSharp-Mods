using mitanpc.Common;
using mitanpc.Content.Items.Accessories;
using mitanpc.Content.Items.Potions;
using mitanpc.Content.Items.Weapons.Melee;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace mitanpc.Content.NPCs.TownNPCs.Mitas.Mita;

[AutoloadHead]
public class Mita : TownNPCBase
{
    public override void SetDefaults()
    {
        base.SetDefaults();
        NPC.width = 40;
        NPC.height = 48;
    }

    public override void AddShops()
    {
        new NPCShop(Type, "Shop")
            .Add<GameCartridge>()
            .Add<Carrot>()
            .Add<TravelRing>(new Condition("Mods.mitanpc.Conditions.AllMechBosses", () => Main.hardMode && NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3))
            .Add(new Item(ItemID.PsychoKnife) { shopCustomPrice = 2500000 }, new Condition("Mods.mitanpc.Conditions.HardmodeBloodMoon", () => Main.hardMode && Main.bloodMoon))
            .Register();
    }
}
