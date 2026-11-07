using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;
using TouhouPetsMod.Content.Buffs.PetBuffs;
using TouhouPetsMod.Content.Projectiles.Pets;

namespace TouhouPetsMod.Content.Items.PetItems;

public class KoishiTelephone : BasicPetItem
{
    protected override int PetType => ModContent.ProjectileType<Koishi>();
    protected override int PetBuffType => ModContent.BuffType<KoishiBuff>();
    protected override (int Width, int Height) Size => (26, 34);

    // The second line is the warning, in red. The original used a [c/ff0000:]
    // tag; written here, the color follows the line in every language.
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        TooltipLine? warning = tooltips.Find(line => line.Name == "Tooltip1");
        if (warning != null)
            warning.OverrideColor = new Color(255, 0, 0);
    }
}
