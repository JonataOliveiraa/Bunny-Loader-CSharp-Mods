using ChestPeekMod.Lib;
using Terraria.ModLoader;

// Chest Peek: shows the contents of a chest when pointing at it.
public class ChestPeek : Mod
{
    public static ChestPeek Instance = null!;

    public override void Load()
    {
        Instance = this;
        ChestPeekPanel.Install();
        Logger.Info("Chest Peek: loaded");
    }
}
