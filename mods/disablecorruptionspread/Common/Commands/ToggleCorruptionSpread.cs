using DisableCorruptionSpreadMod.Common.Systems;
using Terraria.ModLoader;

namespace DisableCorruptionSpreadMod.Common.Commands;

public class ToggleCorruptionSpread : ModCommand
{
    public override string Command => "spread";

    public override CommandType Type => CommandType.World | CommandType.Console;

    public override string Description => "Toggle Corruption Spread";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        BiomeSpreadState.Toggle();
    }
}
