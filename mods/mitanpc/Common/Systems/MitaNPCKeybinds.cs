using Terraria.ModLoader;

namespace mitanpc.Common.Systems;

public class MitaNPCKeybinds : ModSystem
{
    public static ModTouchButton? TravelRingHotKey;

    public override void Load()
    {
        TravelRingHotKey = TouchButtonLoader.RegisterButton(Mod, "TravelRingHotKey");
        TravelRingHotKey.Icon = "Content/Items/Accessories/TravelRing";
    }
}
