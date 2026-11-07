using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using VeinMinerMod.Common.Configs;

namespace VeinMinerMod.Common.Commands;

public class VeinCommand : ModCommand
{
    static readonly Color ReplyColor = new(255, 200, 80);

    public override string Command => "vein";

    public override CommandType Type => CommandType.Chat;

    public override string Usage => Mod.GetLocalization("Commands.VeinCommand.Usage").Value ?? "";

    public override string Description => Mod.GetLocalization("Commands.VeinCommand.Description").Value ?? "";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        void Reply(string key, params object[] values) =>
            caller.Reply(Mod.GetLocalization("Messages." + key).Format(values), ReplyColor);

        Item held = caller.Player.HeldItem;
        bool holdsTile = !held.IsNull && held.type > 0 && held.createTile >= 0;
        string heldName = holdsTile ? held.Name ?? "" : "";

        switch (args.Length > 0 ? args[0] : "list")
        {
            case "add":
                if (!holdsTile)
                    Reply("HoldTile");
                else if (TileList.IsOre(held.createTile) || TileList.IsDefault(held.createTile))
                    Reply("AlreadyDefault", heldName);
                else
                    Reply(TileList.Add(held.createTile, heldName) ? "Added" : "AlreadyAdded", heldName);
                break;
            case "remove":
                if (!holdsTile)
                    Reply("HoldTile");
                else
                    Reply(TileList.Remove(held.createTile) ? "Removed" : "NotInList", heldName);
                break;
            case "list":
                string names = string.Join(", ", TileList.PlayerTileNames);
                Reply(names.Length > 0 ? "List" : "ListEmpty", names);
                break;
            case "clear":
                Reply("Cleared", TileList.Clear());
                break;
            case "on":
            case "off":
                bool enable = args[0] == "on";
                VeinMinerConfig config = VeinMinerConfig.Instance;
                var pending = (VeinMinerConfig)config.Clone();
                pending.Enabled = enable;
                config.SaveChanges(pending);
                Reply(enable ? "Enabled" : "Disabled");
                break;
            default:
                throw new UsageException();
        }
    }
}
