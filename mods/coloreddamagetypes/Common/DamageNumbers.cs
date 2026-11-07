using System;
using System.Collections.Generic;
using BunnyLoader.Interop;
using ColoredDamageTypesMod.Common.Configs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ColoredDamageTypesMod.Common;

public static class Net
{
    public static Mod? Mod;
}

// The game writes a hit's number by itself. This finds that CombatText right after the hit and recolors it.
public static class DamageNumbers
{
    sealed class Hit
    {
        public NPC Npc;
        public DamageClass Class = null!;
        public bool Sentry;
        public int? Damage;
        public bool Others;
        public uint Frame;
    }

    static readonly List<Hit> pending = new();
    static readonly Dictionary<(int WhoAmI, int Type), Dictionary<DamageClass, int[]>> totals = new();
    static readonly Dictionary<int, uint> taken = new();

    static bool broken;

    static uint Frame => Main.GameUpdateCount;

    static bool Same(Color a, Color b) => a.R == b.R && a.G == b.G && a.B == b.B;

    static int Faded(byte value) => (int)MathF.Floor(value * 0.4f + 0.5f);

    static bool FindText(Hit hit, int slack, out CombatText text, out int index, out bool crit)
    {
        GameArray<CombatText> texts = Main.combatText;
        uint now = Frame;
        Vector2 p = hit.Npc.position;
        float x0 = p.X - 48, y0 = p.Y - 64, x1 = p.X + hit.Npc.width + 48, y1 = p.Y + hit.Npc.height + 48;

        for (int i = texts.Length - 1; i >= 0; i--)
        {
            CombatText t = texts[i];
            if (!t.active)
                continue;
            if (taken.TryGetValue(i, out uint at) && now - at < 10)
                continue;
            if (t.alpha < 0.95f)
                continue;

            int life = t.lifeTime;
            bool normal = life <= 60 && life >= 60 - slack;
            bool isLong = life <= 120 && life >= 120 - slack;
            bool dot = t.dot && life <= 40 && life >= 40 - slack;
            Color color = t.color;
            bool isCrit;
            if (hit.Others)
            {
                if (normal && Same(color, CombatText.OthersDamagedHostile))
                    isCrit = false;
                else if (isLong && Same(color, CombatText.OthersDamagedHostileCrit))
                    isCrit = true;
                else
                    continue;
            }
            else
            {
                if (!(normal || isLong || dot))
                    continue;
                if (Same(color, CombatText.DamagedHostile))
                    isCrit = false;
                else if (Same(color, CombatText.DamagedHostileCrit))
                    isCrit = true;
                else
                    continue;
            }

            Vector2 pos = t.position;
            if (pos.X < x0 || pos.X > x1 || pos.Y < y0 || pos.Y > y1)
                continue;
            text = t;
            index = i;
            crit = isCrit;
            return true;
        }
        text = default;
        index = -1;
        crit = false;
        return false;
    }

    // The number's leading digits, as the original read them.
    static int ParseLeadingInt(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
        int value = 0, at = 0;
        bool negative = text[0] == '-';
        if (negative)
            at = 1;
        for (; at < text.Length && char.IsAsciiDigit(text[at]) && value < 100000000; at++)
            value = value * 10 + (text[at] - '0');
        return negative ? -value : value;
    }

    static bool Process(Hit hit)
    {
        ColoredDamageTypesConfig config = ColoredDamageTypesConfig.Instance;
        if (!FindText(hit, hit.Others ? 12 : 3, out CombatText t, out int i, out bool crit))
            return false;

        taken[i] = Frame;

        Color color = DamageTypes.CheckDamageColor(hit.Class, crit, hit.Sentry)
            ?? (crit ? CombatText.DamagedHostileCrit : CombatText.DamagedHostile);
        color.A = 255;
        if (hit.Others)
            color = new Color(Faded(color.R), Faded(color.G), Faded(color.B), Faded(color.A));
        t.color = color;

        if (config.DebugMode && (!hit.Others || config.DebugModeMultiplayer))
            DamageTypes.Log((hit.Others ? "Others " : "Hit ") + DamageTypes.KeyOf(hit.Class) + " crit=" + crit + " text=" + t.text);

        if (!(hit.Others ? config.ShowMultiplayerDamageNumbers : config.ShowDamageNumbers))
        {
            t.active = false;
            return true;
        }

        NPC npc = hit.Npc;
        int damage = hit.Damage ?? ParseLeadingInt(t.text);
        var npcKey = (npc.whoAmI, npc.type);
        if (!totals.TryGetValue(npcKey, out Dictionary<DamageClass, int[]>? byClass))
            totals[npcKey] = byClass = new Dictionary<DamageClass, int[]>();
        if (!byClass.TryGetValue(hit.Class, out int[]? sum))
            byClass[hit.Class] = sum = new int[2];
        sum[0] += damage;

        int hits = config.CondenseDamageHits;
        if (hits <= 0)
            return true;
        if (sum[1] < hits && npc.active)
        {
            sum[1]++;
            t.active = false;
            return true;
        }

        t.text = sum[0].ToString();
        sum[0] = 0;
        sum[1] = 0;
        if (!npc.active && !config.ShowCondenseDamageOnKill)
        {
            t.active = false;
            totals.Remove(npcKey);
        }
        return true;
    }

    static bool Guarded(Hit hit)
    {
        if (broken)
            return false;
        try
        {
            return Process(hit);
        }
        catch (Exception e)
        {
            broken = true;
            DamageTypes.Log("The damage numbers could not be recolored in this version of the game; the feature was disabled.: " + e);
            return false;
        }
    }

    public static void OnHit(NPC npc, DamageClass cls, bool sentry, int? damage)
    {
        if (!ColoredDamageTypesConfig.Instance.ChangeDamageColor)
            return;
        Queue(new Hit { Npc = npc, Class = cls, Sentry = sentry, Damage = damage, Others = false });

        // A client tells the server; the phone's host (netMode 3) tells its clients.
        if ((Main.netMode & NetmodeID.MultiplayerClient) != 0 && Net.Mod != null)
        {
            ModPacket packet = Net.Mod.GetPacket();
            packet.Write(npc.whoAmI);
            packet.Write(DamageTypes.ColorIdOf(cls));
            packet.Write(sentry);
            packet.Send();
        }
    }

    public static void OnOthersHit(NPC npc, DamageClass cls, bool sentry)
    {
        if (!ColoredDamageTypesConfig.Instance.ChangeDamageColor)
            return;
        Queue(new Hit { Npc = npc, Class = cls, Sentry = sentry, Damage = null, Others = true });
    }

    static void Queue(Hit hit)
    {
        hit.Frame = Frame;
        if (!Guarded(hit))
            pending.Add(hit);
    }

    public static void Flush()
    {
        if (pending.Count == 0)
            return;
        uint now = Frame;
        for (int k = pending.Count - 1; k >= 0; k--)
        {
            Hit hit = pending[k];
            bool done = Guarded(hit);
            if (done || broken || now - hit.Frame > (hit.Others ? 20u : 1u))
                pending.RemoveAt(k);
        }
    }

    public static void Clear()
    {
        pending.Clear();
        totals.Clear();
        taken.Clear();
    }
}
