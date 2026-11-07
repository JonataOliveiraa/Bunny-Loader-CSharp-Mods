using Microsoft.Xna.Framework.Graphics;
using ModDifficultyMod.DifficultySystem;
using Terraria;

namespace ModDifficultyMod;

// The "Legendary" difficulty. The class keeps the original's name: its texts are under this key.
public class ExampleDifficulty : ModDifficulty
{
    public ExampleDifficulty() => Texture = "Textures/ExampleDifficulty";

    public override int VanillaEquivalent => DifficultyID.Master;

    public override Color GetColor()
    {
        int value = (int)(Main.masterColor * 200);
        return new Color(value, 255, value);
    }

    public override float GetDifficultyLevel(float currentLevel) => 4;
}
