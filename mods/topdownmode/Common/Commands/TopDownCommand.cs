using System.Globalization;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;
using topdownmode.Common.NPCs;
using topdownmode.Common.Players;
using topdownmode.Common.Systems;
using topdownmode.Content.Effects;

namespace topdownmode.Common.Commands;

// The JavaScript version exposed these on its console (TopDownWorld.build(), TopDownCave.enter()...).
// C# mods have no such console, so they live on a chat command.
public class TopDownCommand : ModCommand
{
    static readonly Color ReplyColor = new(215, 233, 255);

    public override string Command => "topdown";

    public override CommandType Type => CommandType.Chat;

    public override string Usage =>
        "/topdown status | build [seed] | decorate [density] | undecorate | addtrees [density] | blend | unblend | hidedeep | opendeep | enter | leave | viamenu "
        + "| cleanbases [radius] | cleancactus [radius] | audit [radius] | ghosts [radius] | nearby [radius] | addfurniture <type> | addgrass <type>";

    public override string Description => "Maintenance of top-down worlds and the mod's status.";

    static int IntArg(string[] args, int fallback) =>
        args.Length > 1 && int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;

    static double? DoubleArg(string[] args) =>
        args.Length > 1 && double.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : null;

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (args.Length == 0)
            throw new UsageException();
        string reply;
        switch (args[0])
        {
            case "status":
                reply = string.Join("\n", TopDownPlayer.Status(), TopDownCombat.Status(), TopDownTreeBase.Status(), TopDownCactus.Status(), TopDownTreeDepth.Status(), TopDownNPC.Status(),
                    TopDownDrops.Status(), TopDownFurniture.Status(), TopDownProjectiles.Status(), TopDownNoHeat.Status(), TopDownCave.Status(), TopDownWater.Status(), TopDownLook.Status());
                TopDownMode.Log(reply);
                break;
            case "build":
                int? seed = args.Length > 1 && int.TryParse(args[1], out int parsed) ? parsed : null;
                var spawn = TopDownWorldSystem.Build(seed);
                reply = "world rebuilt, spawn " + spawn.X + "," + spawn.Y;
                break;
            case "decorate":
                reply = TopDownWorldSystem.Decorate(DoubleArg(args));
                break;
            case "undecorate":
                reply = TopDownWorldSystem.Undecorate();
                break;
            case "addtrees":
                reply = TopDownWorldSystem.AddTrees(DoubleArg(args));
                break;
            case "blend":
                reply = TopDownWorldSystem.Blend();
                break;
            case "unblend":
                reply = TopDownWorldSystem.Unblend();
                break;
            case "hidedeep":
                TopDownWorldSystem.HideDeep();
                reply = "deep sea glass hidden (see the log).";
                break;
            case "opendeep":
                TopDownWorldSystem.OpenDeep();
                reply = "deep sea opened (see the log).";
                break;
            case "enter":
                reply = TopDownCave.Enter() ? "going down." : "could not go down now.";
                break;
            case "leave":
                reply = TopDownCave.Leave() ? "going up." : "not in the cave.";
                break;
            case "viamenu":
                TopDownCave.ViaMenu = !TopDownCave.ViaMenu;
                reply = "cave travel through the title screen: " + TopDownCave.ViaMenu;
                break;
            case "cleanbases":
                reply = TopDownTreeBase.Clean(IntArg(args, 0));
                break;
            case "cleancactus":
                reply = TopDownCactus.Clean(IntArg(args, 0));
                break;
            case "audit":
                reply = TopDownTreeBase.Audit(IntArg(args, 0));
                break;
            case "ghosts":
                reply = TopDownTreeDepth.Ghosts(IntArg(args, 0));
                break;
            case "nearby":
                reply = TopDownTreeDepth.Nearby(IntArg(args, 0));
                break;
            case "addfurniture":
                reply = TopDownTreeDepth.AddFurnitureTile(IntArg(args, -1)) ? "furniture type added until the game closes." : "that is not a tile type.";
                break;
            case "addgrass":
                reply = TopDownTreeDepth.AddGrassTile(IntArg(args, -1)) ? "grass type added until the game closes." : "that is not a tile type.";
                break;
            default:
                throw new UsageException();
        }
        caller.Reply(reply, ReplyColor);
    }
}
