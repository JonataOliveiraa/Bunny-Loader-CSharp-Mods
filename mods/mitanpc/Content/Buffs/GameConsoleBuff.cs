using mitanpc.Content.Projectiles.Pets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace mitanpc.Content.Buffs;

public class GameConsoleBuff : ModBuff
{
    public override void SetStaticDefaults()
    {
        Main.buffNoTimeDisplay[Type] = true;
        Main.vanityPet[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        bool pet = false;
        player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref pet, ModContent.ProjectileType<FlyPetProjectile>(), 18000);
    }

    // Tapping the icon removes the buff.
    public override bool RightClick(int buffIndex)
    {
        SoundEngine.PlaySound(new SoundStyle("mitanpc/Assets/Sounds/MarioCoinGrabReversed") { Volume = 0.35f });
        return true;
    }

    public override bool PreDraw(SpriteBatch spriteBatch, int buffIndex, ref BuffDrawParams drawParams)
    {
        Texture2D texture = drawParams.Texture;
        int height = texture.Height / 2;
        int frame = (int)(Main.GameUpdateCount / 14 % 2);
        drawParams.SourceRectangle = new Rectangle(0, frame * height, texture.Width, height);
        return true;
    }
}
