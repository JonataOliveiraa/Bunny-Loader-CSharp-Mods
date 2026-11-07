using Microsoft.Xna.Framework.Graphics;
using NPCUnlockAnnouncerMod.Common.UI;
using Terraria.ModLoader;

namespace NPCUnlockAnnouncerMod.Common.Commands;

// The config screen has no buttons, so the Preview button of the JavaScript version is here.
public class AnnouncerCommand : ModCommand
{
    public override string Command => "npcannounce";

    public override CommandType Type => CommandType.Chat;

    public override string Usage => "/npcannounce preview";

    public override string Description => "NPC Unlock Announcer: shows a test banner.";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (args.Length == 0 || args[0] != "preview")
            throw new UsageException();
        Banner.Preview();
        caller.Reply("Showing a test banner.", new Color(255, 215, 0));
    }
}
