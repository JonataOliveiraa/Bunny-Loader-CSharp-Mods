using Microsoft.Xna.Framework;
using Terraria.Audio;

namespace umamusumevanities.Common.Audio;

public static class UmamusumeAudio
{
    const string Root = "umamusumevanities/Assets/Sounds/Umamusume/";

    static SoundStyle sonicBoomSound;
    static SoundStyle mamboSound;
    static bool ready;

    public static void Preload()
    {
        sonicBoomSound = new SoundStyle(Root + "SonicBoom") { Volume = 0.9f, MaxInstances = 2 };
        mamboSound = new SoundStyle(Root + "Mambo") { Volume = 0.9f, MaxInstances = 2 };
        ready = true;
    }

    public static void PlaySonicBoom(Vector2 position)
    {
        if (ready)
            SoundEngine.PlaySound(sonicBoomSound, position);
    }

    public static void PlayMambo(Vector2 position)
    {
        if (ready)
            SoundEngine.PlaySound(mamboSound, position);
    }
}
