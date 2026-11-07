using ColoredDamageTypesMod.Common.Configs;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace ColoredDamageTypesMod.Common.Commands;

// The config screen has no buttons, so the two buttons of the JavaScript version are here.
public class ColoredDamageCommand : ModCommand
{
    static readonly Color ReplyColor = new(255, 160, 80);

    public override string Command => "cdt";

    public override CommandType Type => CommandType.Chat;

    public override string Usage => "/cdt pillar | /cdt reset";

    public override string Description => "Colored Damage Types: applies the Pillar Colors preset, or restores the held weapon's class to the mod's colors.";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        switch (args.Length > 0 ? args[0] : "")
        {
            case "pillar":
                ColoredDamageTypesConfig.Instance.ApplyPillarPreset();
                caller.Reply("Pillar Colors applied.", ReplyColor);
                break;
            case "reset":
                caller.Reply(ColoredDamageTypesConfig.Instance.ResetHeldClass()
                    ? "The held weapon's class is back to the mod's colors."
                    : "Hold a weapon of a modded damage class first.", ReplyColor);
                break;
            default:
                throw new UsageException();
        }
    }
}
