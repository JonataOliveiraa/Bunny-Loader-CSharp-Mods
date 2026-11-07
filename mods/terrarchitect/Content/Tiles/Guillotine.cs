using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace terrarchitect.Content.Tiles;

public class Guillotine : ModTile
{
    private SoundStyle Sound;

    public override void SetStaticDefaults()
    {
        Main.tileFrameImportant[Type] = true;
        Main.tileNoAttach[Type] = true;
        Main.tileLavaDeath[Type] = true;
        TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
        TileObjectData.newTile.Width = 4;
        TileObjectData.newTile.Height = 5;
        TileObjectData.newTile.Origin = new Point16(0, 4);
        TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16, 16, 18 };
        TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;
        TileObjectData.newTile.StyleHorizontal = true;
        TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
        TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
        TileObjectData.addAlternate(1);
        TileObjectData.addTile(Type);
        TileID.Sets.DisableSmartCursor[Type] = true;

        RegisterItemDrop(ModContent.ItemType<Items.GuillotineItem>());
        Sound = new SoundStyle("terrarchitect/Assets/Sounds/Guillotine");
    }

    public override bool RightClick(int x, int y)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return true;

        NPC nearestNPC = default;
        float minDist = float.PositiveInfinity;
        Vector2 center = new Vector2(x * 16, y * 16);
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (!npc.active || !npc.townNPC || npc.type == NPCID.OldMan || npc.aiStyle == 0) continue;

            float distance = npc.DistanceSQ(center);
            if (distance < minDist)
            {
                nearestNPC = npc;
                minDist = distance;
            }
        }
        if (!nearestNPC.IsNull && minDist < 300000)
        {
            SoundEngine.PlaySound(Sound, Main.LocalPlayer.position);
            nearestNPC.StrikeNPCNoInteraction(9999, 10, -nearestNPC.direction);
        }
        return true;
    }

    public override void MouseOver(int i, int j)
    {
        Player player = Main.LocalPlayer;
        player.noThrow = 2;
        player.cursorItemIconEnabled = true;
        player.cursorItemIconID = ModContent.ItemType<Items.GuillotineItem>();
    }
}
