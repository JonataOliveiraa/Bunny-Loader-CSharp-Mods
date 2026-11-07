using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace WeaponOutLiteMod.Common.Commands;

public class WeaponOutCommand : ModCommand
{
    public override string Command => "wo";

    public override CommandType Type => CommandType.Chat;

    public override string Usage => Actions.T("Commands.WeaponOutCommand.Usage");

    public override string Description => Actions.T("Commands.WeaponOutCommand.Description");

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        switch (args.Length > 0 ? args[0].ToLowerInvariant() : "")
        {
            case "":
            case "toggle":
            case "show":
                Actions.ToggleShowItem();
                return;
            case "next":
            case "group":
                Actions.NextPoseGroup();
                return;
            case "pose":
                if (args.Length < 2 || !Actions.ForcePose(args[1]))
                    caller.Reply(Actions.T("Common.UnknownPose"), Color.LightGray);
                return;
            case "turn":
                Actions.TurnItemSprite();
                return;
            case "clear":
                Actions.ClearItemOverride();
                return;
            case "reset":
                Actions.ResetAllOverrides();
                return;
            case "info":
                Actions.Info();
                return;
            case "poses":
            case "list":
                Actions.ListPoses();
                return;
        }
        throw new UsageException();
    }
}

// ModCommand has no aliases: /weaponout is the same command under its second name.
public class WeaponOutAliasCommand : WeaponOutCommand
{
    public override string Command => "weaponout";
}
