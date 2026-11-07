using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace topdownmode.Content.Walls;

public class TopDownSnow : ModWall
{
    public override void SetStaticDefaults()
    {
        Main.wallHouse[Type] = false;
        Main.wallLight[Type] = true;
        WallID.Sets.AllowsPlantsToGrow[Type] = true;
        VanillaFallbackOnModDeletion = WallID.DirtUnsafe;
        AddMapEntry(new Color(232, 240, 247));
    }
}
