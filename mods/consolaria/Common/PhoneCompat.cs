// What upstream takes from tModLoader or the PC game and the phone names differently or
// lacks. Types declared in namespace Consolaria win over the using directives at the top
// of the copied files, so those files stay as upstream wrote them.
using BunnyLoader.Interop;

using Microsoft.Xna.Framework;

using System;

using Terraria;
using Terraria.DataStructures;
using Terraria.Utilities;

namespace Consolaria {
    // The default Texture comes from the namespace, "Consolaria/Content/...", while the mod is
    // called "consolaria". Mod.Name comparisons in the loader (ModContent.Request,
    // EquipLoader.FileOf) are case sensitive, so the first segment has to follow the mod name.
    internal static class ContentPath {
        public static string Of(string defaultPath)
            => defaultPath.StartsWith("Consolaria/", StringComparison.Ordinal) ? "consolaria/" + defaultPath["Consolaria/".Length..] : defaultPath;
    }

    public abstract class ModItem : Terraria.ModLoader.ModItem {
        private string texture;
        public override string Texture => texture ??= ContentPath.Of(base.Texture);
    }

    public abstract class ModNPC : Terraria.ModLoader.ModNPC {
        private string texture;
        public override string Texture => texture ??= ContentPath.Of(base.Texture);
    }

    public abstract class ModProjectile : Terraria.ModLoader.ModProjectile {
        private string texture;
        public override string Texture => texture ??= ContentPath.Of(base.Texture);
    }

    // The proxy of Terraria.BitsByte only exposes the raw value.
    public struct BitsByte {
        public byte value;

        public bool this[int key] {
            readonly get => (value & (1 << key)) != 0;
            set {
                if (value) this.value |= (byte)(1 << key);
                else this.value &= (byte)~(1 << key);
            }
        }

        public static implicit operator byte(BitsByte bits) => bits.value;
        public static implicit operator BitsByte(byte value) => new() { value = value };
    }

    internal static class PhoneExtensions {
        // Recipe.SortAfter and friends do not exist on the phone: recipes keep the order they were added in.
        public static Recipe SortAfter(this Recipe recipe, Recipe other) => recipe;
        public static Recipe SortAfterFirstRecipesOf(this Recipe recipe, int itemId) => recipe;
        public static Recipe SortBeforeFirstRecipesOf(this Recipe recipe, int itemId) => recipe;

        public static void SetDefaults(this Item item, int type) => item.SetDefaults(type, default);

        public static IEntitySource GetSource_Misc(this Player player, string context) => player.GetSource_FromThis(context);
        public static IEntitySource GetSource_OpenItem(this Player player, int itemType) => player.GetSource_FromThis("OpenItem");
        public static IEntitySource GetSource_ItemUse(this Player player, Item item) => player.GetSource_FromThis("ItemUse");

        public static Vector2 ToVector2(this Point point) => new(point.X, point.Y);
        public static float AngleTo(this Vector2 from, Vector2 to) => (float)Math.Atan2(to.Y - from.Y, to.X - from.X);
        public static float Distance(this Vector2 from, Vector2 to) => Vector2.Distance(from, to);
        public static Color MultiplyRGB(this Color first, Color second) => Utils.MultiplyRGB(first, second);

        public static T NextFromList<T>(this UnifiedRandom random, params T[] items) => items[random.Next(items.Length)];
    }

    // Dust.customData is an object in the game, and upstream stores boxed numbers in it.
    internal static unsafe class GameBox {
        private static readonly GameClass Int32Class = new("System", "Int32");
        private static readonly GameClass SingleClass = new("System", "Single");
        private const int ObjectHeader = 16;

        public static GameObject Of(int value) {
            IntPtr box = GameInterop.NewObject(Int32Class);
            *(int*)(box + ObjectHeader) = value;
            return new GameObject(box);
        }

        public static GameObject Of(float value) {
            IntPtr box = GameInterop.NewObject(SingleClass);
            *(float*)(box + ObjectHeader) = value;
            return new GameObject(box);
        }
    }

    // Conditions tModLoader has and the phone's Condition lacks.
    internal static class ConsolariaConditions {
        public static readonly Condition BloodMoon = new("Conditions.BloodMoon", () => Main.bloodMoon);
        public static readonly Condition Christmas = new("Conditions.Christmas", () => Main.xMas);
        public static readonly Condition MoonPhaseFull = new("Conditions.MoonPhaseFull", () => Main.moonPhase == 0);
        public static readonly Condition MoonPhaseNew = new("Conditions.MoonPhaseNew", () => Main.moonPhase == 4);
        public static readonly Condition DownedMechBossAny = new("Conditions.DownedMechBossAny", () => NPC.downedMechBossAny);
    }
}
