using MenuMediaPort.Lib;
using Terraria.ModLoader;

namespace MenuMediaPort.Common.Commands;

// "/menumedia": the themes found, the state and the music of each.
public class MenuMediaCommand : ModCommand
{
    public override string Command => "menumedia";

    public override CommandType Type => CommandType.Chat;

    public override string Description => "Menu Media: lists the menu themes, their state and their music.";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        foreach (string line in MenuMedia.Describe())
            caller.Reply(line);
    }
}
