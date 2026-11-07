// The base of the pet items: a vanity or light pet item, orange, worth 200
// gold (DefaultToVanitypet + DefaultToVanitypetExtra of the original); one
// unlocks it in Journey mode (Common/ResearchSystem.cs). A base, not an item.
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace TouhouPetsMod.Content.Items;

public abstract class BasicPetItem : ModItem
{
    // The pet; none for the pairs, whose buff calls both.
    protected virtual int PetType => 0;
    protected abstract int PetBuffType { get; }
    protected virtual (int Width, int Height) Size => (26, 26);

    public override void SetDefaults()
    {
        Item.DefaultToVanitypet(PetType, PetBuffType);
        Item.width = Size.Width;
        Item.height = Size.Height;
        Item.rare = ItemRarityID.Orange;
        Item.value = 2000000;
    }

    // The game gives the buff of its own pet items by item type; a mod item
    // gives it here. The buff brings the pet.
    public override bool? UseItem(Player player)
    {
        if (player.whoAmI == Main.myPlayer)
            player.AddBuff(Item.buffType, 3600, false);
        return null;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) => false;
}
