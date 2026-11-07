using Terraria;
using Terraria.ModLoader;

namespace DefaultDashMod
{
    public class DefaultDash : Mod
    {
        private bool[] prevLeft = new bool[256];
        private bool[] prevRight = new bool[256];
        private int[] lastLeftTapTime = new int[256];
        private int[] lastRightTapTime = new int[256];

        private int[] dashTimer = new int[256];
        private int[] dashCooldown = new int[256];
        private int[] dashDirection = new int[256];

        public override void Load()
        {
            On_Player.Update += (orig, self, i) =>
            {
                orig(self, i);

                if (!self.active || self.dead || self.dashType > 0)
                    return;

                int idx = self.whoAmI;
                int now = System.Environment.TickCount;

                if (dashCooldown[idx] > 0) dashCooldown[idx]--;
                if (dashTimer[idx] > 0) dashTimer[idx]--;

                // Only X changes, so jumping and flying are not interrupted.
                if (dashTimer[idx] > 0)
                    self.velocity.X = dashDirection[idx] * 7f;

                if (dashCooldown[idx] == 0 && dashTimer[idx] == 0)
                {
                    if (self.controlLeft && !prevLeft[idx])
                    {
                        if (now - lastLeftTapTime[idx] < 300 && now - lastLeftTapTime[idx] > 50)
                        {
                            StartDash(self, idx, -1);
                            lastLeftTapTime[idx] = 0;
                        }
                        else
                        {
                            lastLeftTapTime[idx] = now;
                        }
                    }
                    prevLeft[idx] = self.controlLeft;

                    if (self.controlRight && !prevRight[idx])
                    {
                        if (now - lastRightTapTime[idx] < 300 && now - lastRightTapTime[idx] > 50)
                        {
                            StartDash(self, idx, 1);
                            lastRightTapTime[idx] = 0;
                        }
                        else
                        {
                            lastRightTapTime[idx] = now;
                        }
                    }
                    prevRight[idx] = self.controlRight;
                }
            };
        }

        private void StartDash(Player player, int idx, int direction)
        {
            dashTimer[idx] = 10;
            dashCooldown[idx] = 60;
            dashDirection[idx] = direction;

            player.velocity.X = direction * 7f;
        }
    }
}
