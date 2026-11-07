using ModDifficultyMod;
using ModDifficultyMod.DifficultySystem;
using Terraria.ModLoader;

// Adds custom world difficulties (the game only has Normal, Expert, Master and Journey).
public class ModDifficultyBunnyLoader : Mod
{
    public override void Load()
    {
        DifficultyHooks.Initialize();
        WorldCreateMenu.Initialize();
        ModDifficulty.Register(new ExampleDifficulty());
    }
}
