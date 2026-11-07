using mitanpc.Common.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace mitanpc.Common.Players;

public class MitaNPCPlayer : ModPlayer
{
    public bool travelRing;
    public bool martialArtsManualUsed;
    public bool shootersManualUsed;
    public bool pokemonManualUsed;
    public bool magiciansManualUsed;

    public override void ResetEffects() => travelRing = false;

    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        ModTouchButton? hotKey = MitaNPCKeybinds.TravelRingHotKey;
        if (hotKey == null || !hotKey.JustPressed || !travelRing || Player.dead ||
            Player.whoAmI != Main.myPlayer || Player.FindBuffIndex(BuffID.ChaosState) >= 0)
            return;
        Vector2 cursor = Main.MouseWorld;
        float x = cursor.X - Player.width / 2f;
        float y = Player.gravDir == 1 ? cursor.Y - Player.height : cursor.Y;
        if (!float.IsFinite(x) || !float.IsFinite(y) || x <= 50 || y <= 50 ||
            x >= Main.maxTilesX * 16 - 50 || y >= Main.maxTilesY * 16 - 50)
            return;
        var destination = new Vector2(x, y);
        if (Collision.SolidCollision(destination, Player.width, Player.height))
            return;
        Player.Teleport(destination, 1, 0);
        if (Main.netMode != NetmodeID.SinglePlayer)
            NetMessage.SendData(MessageID.TeleportEntity, -1, -1, default, 0, Player.whoAmI, x, y, 1, 0, 0);
        Player.AddBuff(BuffID.ChaosState, 1200, false);
    }

    public override void PostUpdateEquips()
    {
        if (martialArtsManualUsed || pokemonManualUsed)
            Player.autoReuseGlove = true;
    }

    public override void ModifyWeaponDamage(Item item, ref StatModifier damage)
    {
        bool boosted = (item.melee && martialArtsManualUsed) ||
            (item.ranged && shootersManualUsed) ||
            (item.magic && magiciansManualUsed) ||
            (item.summon && pokemonManualUsed);
        if (boosted)
            damage *= 1.1f;
    }

    public override void SaveData(TagCompound tag)
    {
        tag["martialArtsManualUsed"] = martialArtsManualUsed;
        tag["shootersManualUsed"] = shootersManualUsed;
        tag["pokemonManualUsed"] = pokemonManualUsed;
        tag["magiciansManualUsed"] = magiciansManualUsed;
    }

    public override void LoadData(TagCompound tag)
    {
        martialArtsManualUsed = tag.GetBool("martialArtsManualUsed");
        shootersManualUsed = tag.GetBool("shootersManualUsed");
        pokemonManualUsed = tag.GetBool("pokemonManualUsed");
        magiciansManualUsed = tag.GetBool("magiciansManualUsed");
    }
}
