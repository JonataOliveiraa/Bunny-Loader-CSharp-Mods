using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace topdownmode.Content.Walls;

public class TopDownCaveFloor : ModWall
{
    public override void SetStaticDefaults()
    {
        Main.wallHouse[Type] = false;
        Main.wallLight[Type] = false;
        VanillaFallbackOnModDeletion = WallID.Stone;
        AddMapEntry(new Color(58, 58, 68));
    }
}
