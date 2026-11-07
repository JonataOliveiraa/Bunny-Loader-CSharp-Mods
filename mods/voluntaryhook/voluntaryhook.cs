// Copyright (C) 2026 lzup333
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.
//
// Modified on 2026-11-06: ported from JavaScript to C#.

using System;
using Terraria;
using Terraria.ModLoader;

public class VoluntaryHook : Mod
{
    // Four extra copies give five bobbers in total.
    const int ExtraCopies = 4;
    // Angle between neighboring copies, in radians (about 5.7 degrees).
    const float SpreadStep = 0.10f;

    bool loggedOnce;

    public override void Load()
    {
        On_Projectile.NewProjectile_IEntitySource_float_float_float_float_int_int_float_int_float_float_float_NewProjectileModifier +=
            (orig, spawnSource, x, y, speedX, speedY, type, damage, knockBack, owner, ai0, ai1, ai2, modifier) =>
            {
                int index = orig(spawnSource, x, y, speedX, speedY, type, damage, knockBack, owner, ai0, ai1, ai2, modifier);

                if (owner != Main.myPlayer)
                    return index;

                Projectile projectile = Main.projectile[index];
                if (projectile.IsNull || !projectile.bobber)
                    return index;

                // Only the velocity turns, in a symmetric fan, so the copies land apart.
                for (int j = 0; j < ExtraCopies; j++)
                {
                    float angle = (j - (ExtraCopies - 1) * 0.5f) * SpreadStep;
                    float cos = MathF.Cos(angle);
                    float sin = MathF.Sin(angle);
                    orig(spawnSource, x, y, speedX * cos - speedY * sin, speedX * sin + speedY * cos, type, damage, knockBack, owner, ai0, ai1, ai2, modifier);
                }

                if (!loggedOnce)
                {
                    loggedOnce = true;
                    Logger.Info($"VoluntaryHook: 5x bobbers working (owner={owner}, type={type})");
                }
                return index;
            };

        Logger.Info("VoluntaryHook: active");
    }
}
