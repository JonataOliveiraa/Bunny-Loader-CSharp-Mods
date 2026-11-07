using System.ComponentModel;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace CoyoteframesMod.Common;

public class CoyoteModPlayer : ModPlayer
{
    public int CoyoteTimeFrames;
    public bool IsCoyoteTimeFrames;
    public bool IsJumpAlreadyMade;

    public override void PreUpdate()
    {
        if (Collision.TileCollision(Player.position, Player.velocity, Player.width, Player.height, true, false, (int)Player.gravDir, false, false, true).Y == 0)
        {
            CoyoteTimeFrames = CoyoteConfig.Instance.CoyoteTimeFramesDefault;
            IsCoyoteTimeFrames = false;
            IsJumpAlreadyMade = false;
        }
        else if (CoyoteTimeFrames > 0)
        {
            CoyoteTimeFrames--;
            IsCoyoteTimeFrames = true;
        }
    }

    public override void PostUpdateEquips()
    {
        if (Player.whoAmI == Main.myPlayer && CoyoteTimeFrames > 0 && IsCoyoteTimeFrames && Player.controlJump && Player.releaseJump && !IsJumpAlreadyMade)
        {
            IsJumpAlreadyMade = true;
            Player.velocity.Y = -Player.jumpSpeed * Player.gravDir;
            Player.jump = Player.jumpHeight;
        }
    }
}

public class CoyoteConfig : ModConfig
{
    public static CoyoteConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("Frames")]
    [Range(0, 18)]
    [Increment(1)]
    [DefaultValue(9)]
    public int CoyoteTimeFramesDefault;
}
