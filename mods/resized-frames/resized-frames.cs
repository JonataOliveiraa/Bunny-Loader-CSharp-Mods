using Terraria.ModLoader;
using Terraria.UI;

public class ResizedFrames : Mod
{
    public override void Load()
    {
        On_ItemSlot.DrawItemIcon += (orig, item, ctx, sb, pos, size, maxSize, color, fade, flip) =>
        {
            if (ctx == 40)
                maxSize = 1024;
            return orig(item, ctx, sb, pos, size, maxSize, color, fade, flip);
        };
    }
}
