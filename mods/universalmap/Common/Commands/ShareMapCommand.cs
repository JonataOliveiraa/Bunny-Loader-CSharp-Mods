using Terraria.ModLoader;

namespace UniversalMapMod.Common.Commands;

// The original's "Share Now" button: the config screen has no buttons in C#.
public class ShareMapCommand : ModCommand
{
    public override string Command => "sharemap";

    public override CommandType Type => CommandType.Chat;

    public override string Usage => "/sharemap";

    public override string Description => "Sends your whole explored map right away.";

    public override void Action(CommandCaller caller, string input, string[] args) => Sync.SyncNow();
}
