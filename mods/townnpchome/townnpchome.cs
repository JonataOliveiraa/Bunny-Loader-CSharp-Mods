using System;
using Terraria;
using Terraria.Chat;
using Terraria.ModLoader;

public class TownNPCHome : Mod
{
    static bool forceTeleportNPCs;

    static bool IsValidTownNPC(NPC npc) => !npc.IsNull && npc.active && npc.townNPC && !npc.homeless;

    static void NPCToHome(NPC npc)
    {
        int tileX = (int)Math.Floor(npc.position.X / 16);
        int tileY = (int)Math.Floor(npc.position.Y / 16);
        if (tileX != npc.homeTileX || tileY != npc.homeTileY)
            npc.AI_007_TownEntities_TeleportToHome(npc.homeTileX, npc.homeTileY);
    }

    static void TeleportAllNPCs()
    {
        for (int index = 0; index < Main.maxNPCs; index++)
        {
            NPC npc = Main.npc[index];
            if (IsValidTownNPC(npc))
                NPCToHome(npc);
        }
    }

    static void NPCsHomeChange()
    {
        for (int index = 0; index < Main.maxNPCs; index++)
        {
            NPC npc = Main.npc[index];
            if (IsValidTownNPC(npc) && (npc.homeTileX != npc.oldHomeTileX || npc.homeTileY != npc.oldHomeTileY))
            {
                NPCToHome(npc);
                npc.oldHomeTileX = npc.homeTileX;
                npc.oldHomeTileY = npc.homeTileY;
            }
        }
    }

    public override void Load()
    {
        On_Main.DoUpdate += (orig, self, gameTime) =>
        {
            orig(self, gameTime);

            if (forceTeleportNPCs)
            {
                TeleportAllNPCs();
                forceTeleportNPCs = false;
            }

            NPCsHomeChange();
        };

        On_ChatCommandProcessor.ProcessIncomingMessage += (orig, self, message, clientId) =>
        {
            string? text = message.IsNull ? null : message.Text;
            if (text == null)
            {
                orig(self, message, clientId);
                return;
            }

            string[] parts = text.Trim().Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 && parts[0].ToLowerInvariant() == "/npchome")
            {
                forceTeleportNPCs = true;
                Main.NewText(GetLocalization("teleportedTownNPCs").Value, 0, 255, 255);
                message.Consume();
                return;
            }

            orig(self, message, clientId);
        };
    }
}
