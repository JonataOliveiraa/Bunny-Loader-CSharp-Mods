using Terraria.ModLoader;
using umamusumevanities.Common.Audio;
using umamusumevanities.Common.Players;

public class UmamusumeVanities : Mod
{
    public override void Load()
    {
        UmamusumeAudio.Preload();
        OguriCapAfterimage.Install();
    }
}
