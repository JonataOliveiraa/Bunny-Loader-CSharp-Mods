using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace topdownmode.Content.Walls;

public class TopDownEdgeGrassSnowM8 : ModWall
{
    public override void SetStaticDefaults()
    {
        Main.wallHouse[Type] = false;
        Main.wallLight[Type] = true;
        WallID.Sets.AllowsPlantsToGrow[Type] = true;
        VanillaFallbackOnModDeletion = WallID.DirtUnsafe;
        AddMapEntry(new Color(88, 160, 64));
    }
}
