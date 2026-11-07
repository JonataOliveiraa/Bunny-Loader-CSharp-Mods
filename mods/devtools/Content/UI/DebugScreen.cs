using System;
using System.Globalization;
using DevToolMod.TL;
using DevToolMod.TL.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace DevToolMod.Content.UI;

public class DebugScreen : ModInterface
{
    public static bool IsActive;

    bool active;
    Color BGColor;
    Rectangle Panel;
    Vector2 Position;

    void CheckActive() => active = IsActive;

    public override void SetupContent()
    {
        BGColor = new Color(0, 0, 0, 100);
        Panel = new Rectangle(-80, -80, 0, 0);
        Position = new Vector2(20, 0);
    }

    // Values print as in the JavaScript version: invariant culture and lowercase booleans.
    static string B(bool value) => value ? "true" : "false";

    static string F(float value, int digits) => value.ToString("F" + digits, CultureInfo.InvariantCulture);

    static string I(FormattableString text) => text.ToString(CultureInfo.InvariantCulture);

    static string GetDebugInfo()
    {
        Player player = Main.player[Main.myPlayer];
        string[] worldInfo =
        {
            I($"World: {Main.worldName} | ID: {Main.worldID}"),
            I($"day: {B(Main.dayTime)} | dayRate: {Main.dayRate} | time: {Main.time} | moon: {Main.moonPhase}"),
            I($"spawnX: {Main.spawnTileX * 16} | spawnY: {Main.spawnTileY * 16}"),
            I($"mouseLeft: {B(Main.mouseLeft)} | mouseRight: {B(Main.mouseRight)}"),
            I($"mouseX: {Main.mouseX} | mouseY: {Main.mouseY}"),
            I($"worldMouseLeft: {B(Main.worldMouseLeft)} | worldMouseRight: {B(Main.worldMouseRight)}"),
            I($"worldMouseX: {Main.worldMouseX} | worldMouseY: {Main.worldMouseY}"),
        };
        string[] playerInfo =
        {
            I($"Player: {player.name}"),
            I($"health: {player.statLife}/{player.statLifeMax2} | mana: {player.statMana}/{player.statManaMax2} | defense: {player.statDefense}"),
            I($"spawnX: {player.SpawnX * 16} | spawnY: {player.SpawnY * 16}"),
            I($"posX: {F(player.position.X, 2)} | posY: {F(player.position.Y, 2)}"),
            I($"screenX: {F(Main.screenPosition.X, 2)} | screenY: {F(Main.screenPosition.Y, 2)}"),
            I($"screenWidth: {Main.screenWidth} | screenHeight: {Main.screenHeight}"),
            I($"speedX: {F(player.velocity.X, 4)} | speedY: {F(player.velocity.Y, 4)} | gravity: {F(player.gravity, 2)}"),
            I($"direction: {player.direction} | gravDir: {player.gravDir}"),
            I($"itemPosX: {F(player.itemLocation.X, 2)} | itemPosY: {F(player.itemLocation.Y, 2)} | itemRotation: {F(player.itemRotation, 2)}"),
            I($"isInInventory: {B(Main.playerInventory)} | selectedSlot: {player.selectedItem} | shopIndex: {Main.npcShop}"),
            I($"up: {B(player.controlUp)} | down: {B(player.controlDown)} | left: {B(player.controlLeft)} | right: {B(player.controlRight)}"),
            I($"jump: {B(player.controlJump)} | useItem: {B(player.controlUseItem)} | useTile: {B(player.controlUseTile)} | channel: {B(player.channel)} | altFunction: {player.altFunctionUse}"),
        };
        return string.Join("\n", worldInfo) + "\n\n" + string.Join("\n", playerInfo);
    }

    public override void Draw()
    {
        CheckActive();
        if (!active) return;

        Panel.Width = Main.screenWidth + 160;
        Panel.Height = Main.screenHeight + 160;

        DrawHelper.Rectangle(TextureAssets.MagicPixel.Value, Panel, BGColor);

        DrawHelper.String(DrawHelper.MouseTextFont(), GetDebugInfo(), Position, Color.White, 0.825f);
    }
}
