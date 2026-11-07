using Terraria.ModLoader;
using WorldGenPreviewerMod.Common;
using WorldGenPreviewerMod.Common.Gen;
using WorldGenPreviewerMod.Common.UI;

public class WorldGenPreviewer : Mod
{
    public override void Load()
    {
        Util.mod = this;
        Control.InstallControl();
        Screen.InstallScreen();
        Control.InstallWorldHooks();
    }
}
