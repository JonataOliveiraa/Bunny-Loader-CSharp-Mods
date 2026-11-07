using GnorpBuilderMod.Common;
using GnorpBuilderMod.Content.NPCs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace GnorpBuilderMod.Content.Items;

public class GnorpsPilot : ModItem
{
    public override string Texture => Util.ModName + "/Content/Items/" + Name;

    public override void SetDefaults()
    {
        Item.width = 28;
        Item.height = 34;
        Item.maxStack = 1;
        Item.rare = 2;
        Item.useStyle = 4;
        Item.useAnimation = 20;
        Item.useTime = 20;
        Item.consumable = true;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.UseSound = SoundID.Item90;
        Item.makeNPC = (short)ModContent.NPCType<Gnorp>();
    }

    public override bool CanUseItem(Player player) => !NPC.AnyNPCs(ModContent.NPCType<Gnorp>());
}
