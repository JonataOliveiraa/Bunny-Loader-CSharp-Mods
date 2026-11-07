using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace InfinityPotionBuffMod.Common.Players;

public class InfinityPlayer : ModPlayer
{
    // Item id -> buff id.
    static readonly Dictionary<int, int> ItemBuff = CreateItemBuffs();

    const int Duration = 36000;      // 10 minutes, in frames
    const int CheckEvery = 10;       // checks the Piggy Bank every 10 frames
    const int ItemRefresh = 1800;    // reapplies item and tile buffs every 30 s
    const int PotionRefresh = 900;   // reapplies potion buffs every 15 s
    const int PotionStack = 30;      // smallest potion stack that grants the buff

    int tick;

    static Dictionary<int, int> CreateItemBuffs()
    {
        var map = new Dictionary<int, int>
        {
            [966] = 87,    // Campfire
            [63] = 146,    // Sunflower
            [1859] = 89,   // Heart Lantern
            [2177] = 93,   // Ammo Box
            [2999] = 150,  // Bewitching Table
            [1431] = 158,  // Star in a Bottle
            [3198] = 159,  // Sharpening Station
            [3750] = 192,  // Slice of Cake
            [4276] = 215,  // Bast Statue
            [3814] = 348,  // War Table
            [487] = 29,    // Crystal Ball
            [3117] = 157,  // Peace Candle
            [5322] = 350,  // Shadow Candle
            [148] = 86,    // Water Candle
            [5096] = 336,  // Ham Bat -> Hearty Meal
            [5323] = 165,  // Guide to Peaceful Coexistence -> Dryad's Blessing
        };
        // Any kite -> Windy Day.
        int[] kites = { 4367, 4368, 4369, 4370, 4371, 4379, 4610, 4611, 4612, 4613, 4648, 4649,
            4650, 4651, 4669, 4670, 4671, 4674, 4675, 4676, 4677, 4681, 4683, 4684 };
        foreach (int kite in kites)
            map[kite] = 383;
        return map;
    }

    public override void PostUpdate()
    {
        if (Player.whoAmI != Main.myPlayer)
            return;
        tick++;
        if (tick % CheckEvery != 0 || !Player.active || Player.dead)
            return;

        var slots = Player.bank.item;
        // Buff -> reapply even if the player already has it.
        var wanted = new Dictionary<int, bool>();
        for (int i = 0; i < slots.Length; i++)
        {
            Item item = slots[i];
            int stack = item.stack;
            if (stack < 1)
                continue;

            if (ItemBuff.TryGetValue(item.type, out int mapped))
                wanted.TryAdd(mapped, tick % ItemRefresh == 0);

            if (stack >= PotionStack && item.buffType != 0)
                wanted.TryAdd(item.buffType, tick % PotionRefresh == 0);
        }

        foreach (var (buff, refresh) in wanted)
        {
            if (refresh || !HasBuff(buff))
                Player.AddBuff(buff, Duration, false);
        }
    }

    bool HasBuff(int type)
    {
        var types = Player.buffType;
        var times = Player.buffTime;
        for (int i = 0; i < types.Length; i++)
        {
            if (types[i] == type && times[i] > 0)
                return true;
        }
        return false;
    }
}
