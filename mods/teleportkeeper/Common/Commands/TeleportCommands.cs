using Terraria;
using Terraria.ModLoader;

namespace TeleportKeeperMod.Common.Commands;

public class TpCommand : ModCommand
{
    public override string Command => "tp";

    public override CommandType Type => CommandType.Chat;

    public override string Usage => Util.L("Commands.TpCommand.Usage");

    public override string Description => Util.L("Commands.TpCommand.Description");

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        Player p = Util.Local;
        if (!Data.loaded || p.IsNull)
            return;
        string icon = Icons.BiomeIcon(p);
        string name = args.Length > 0 ? string.Join(" ", args) : Teleports.DefaultName(icon);
        Teleports.Add(name, icon, false, p.position.X / 16f, p.position.Y / 16f);
        Util.Say("NewTpAdded", 50, 255, 130);
    }
}

public class DelTpCommand : ModCommand
{
    public override string Command => "deltp";

    public override CommandType Type => CommandType.Chat;

    public override string Usage => Util.L("Commands.DelTpCommand.Usage");

    public override string Description => Util.L("Commands.DelTpCommand.Description");

    // Reads the digits a text starts with, as the original's parseInt did.
    static bool ParseLeadingInt(string text, out int value)
    {
        value = 0;
        int i = text.Length > 0 && (text[0] == '-' || text[0] == '+') ? 1 : 0;
        int start = i;
        for (; i < text.Length && text[i] >= '0' && text[i] <= '9' && value < 100000000; i++)
            value = value * 10 + (text[i] - '0');
        if (i == start)
            return false;
        if (text[0] == '-')
            value = -value;
        return true;
    }

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (!Data.loaded)
            return;
        if (Data.points.Count == 0)
        {
            Util.Say("DontHaveTps", 255, 0, 0);
            return;
        }
        if (args.Length == 0)
        {
            Util.Say("WrongUsage", 255, 0, 0);
            return;
        }
        if (!ParseLeadingInt(args[0], out int number) || number < 1 || number > Data.points.Count)
        {
            Util.Say("WrongTpNumber", 255, 0, 0);
            return;
        }
        Teleports.Delete(Data.points[number - 1].id);
        Util.Say("RemovedTp", 255, 100, 100);
    }
}

public class ClearAllCommand : ModCommand
{
    public override string Command => "clearall";

    public override CommandType Type => CommandType.Chat;

    public override string Usage => Util.L("Commands.ClearAllCommand.Usage");

    public override string Description => Util.L("Commands.ClearAllCommand.Description");

    public override void Action(CommandCaller caller, string input, string[] args)
    {
        if (!Data.loaded)
            return;
        if (Data.points.Count == 0)
        {
            Util.Say("DontHaveTps", 255, 0, 0);
            return;
        }
        Teleports.Clear();
        Util.Say("RemoveAllTps", 255, 100, 100);
    }
}
