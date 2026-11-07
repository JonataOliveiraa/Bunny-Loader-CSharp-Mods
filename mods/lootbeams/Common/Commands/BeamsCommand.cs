using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using LootBeamsMod.Common.Configs;
using Terraria;
using Terraria.ModLoader;

namespace LootBeamsMod.Common.Commands;

public class BeamsCommand : ModCommand
{
    static readonly Color ReplyColor = new(255, 220, 120);

    public override string Command => "beams";

    public override CommandType Type => CommandType.Chat;

    public override string Usage => Mod.GetLocalization("Commands.BeamsCommand.Usage").Value ?? "";

    public override string Description => Mod.GetLocalization("Commands.BeamsCommand.Description").Value ?? "";

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        void Reply(string key, params object[] values) =>
            caller.Reply(Mod.GetLocalization("Messages." + key).Format(values), ReplyColor);

        string sub = (args.Length > 0 && args[0].Length > 0 ? args[0] : "list").ToLowerInvariant();
        Item item = Main.LocalPlayer.HeldItem;
        bool holds = !item.IsNull && item.type > 0;
        int type = holds ? item.type : 0;
        string name = holds ? Lists.NameOf(item) : "";

        switch (sub)
        {
            case "always":
            case "never":
                if (!holds)
                {
                    Reply("HoldItem");
                    break;
                }
                Lists.Put(sub == "always", type, name);
                Reply(sub == "always" ? "Always" : "Never", name);
                break;
            case "mini":
                if (!holds)
                {
                    Reply("HoldItem");
                    break;
                }
                ListData data = Lists.Data();
                bool on = !data.Mini.Contains(type);
                if (on)
                    data.Mini.Add(type);
                else
                    data.Mini.Remove(type);
                data.Names[type] = name;
                Lists.Save();
                Reply(on ? "MiniOn" : "MiniOff", name);
                break;
            case "color":
                if (!holds)
                {
                    Reply("HoldItem");
                    break;
                }
                if (Util.ParseHex(args.Length > 1 ? args[1] : null) is not Rgb rgb)
                {
                    Reply("BadColor");
                    break;
                }
                Lists.SetColor(type, rgb, name);
                Reply("Colored", name, Util.ToHex(rgb));
                break;
            case "reset":
                if (!holds)
                {
                    Reply("HoldItem");
                    break;
                }
                Reply(Lists.Forget(type) ? "Reset" : "NotListed", name);
                break;
            case "list":
                ListData d = Lists.Data();
                string NameOf(int t) => d.Names.TryGetValue(t, out string? known) && known.Length > 0 ? known : "#" + t;
                string Names(List<int> list) => list.Count > 0 ? string.Join(", ", list.Select(NameOf)) : "-";
                string colors = d.Colors.Count > 0 ? string.Join(", ", d.Colors.Select(c => NameOf(c.Key) + " " + Util.ToHex(c.Value))) : "-";
                Reply("List", Names(d.Whitelist), Names(d.Blacklist), Names(d.Mini), colors);
                break;
            case "clear":
                Lists.ClearAll();
                Reply("Cleared");
                break;
            case "on":
            case "off":
                bool enable = sub == "on";
                LootBeamConfig config = LootBeamConfig.Instance;
                var pending = (LootBeamConfig)config.Clone();
                pending.Enabled = enable;
                config.SaveChanges(pending);
                Reply(enable ? "Enabled" : "Disabled");
                break;
            default:
                throw new UsageException();
        }
    }
}
