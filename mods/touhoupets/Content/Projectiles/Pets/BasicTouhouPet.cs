// The base of every pet (BasicTouhouPet.cs): the chat bubble, conversations
// between pets, the main timer and the movement helpers. A base, not a pet.
//
// What changed from the original:
//  - The chat state (index, time left, cooldown) lives in fields of the pet,
//    not in Projectile.ai/localAI, and the pets find each other in a list of
//    the live ones. The original scanned all of Main.projectile for every
//    lookup, several times a frame per pet.
//  - The lines come from Localization (Chat.<Pet>.<key>). How long a line
//    stays counts a Chinese character as one and a letter as about a third,
//    so an English or Portuguese line lasts about as long as the original.
//  - PetState is a field too (it was Projectile.ai[1]).
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;
using TouhouPetsMod.Common;

namespace TouhouPetsMod.Content.Projectiles.Pets;

// A line of a pet: its index, the key under Chat.<Pet> (null says nothing) and its weight.
// The index is how the other pets recognize a line.
public readonly record struct ChatLine(int Index, string? Key, float Weight = 1f);

// The common drawing data: texture, frame height, position and flip.
public readonly struct PetDrawInfo
{
    public Texture2D t { get; init; }
    public int width { get; init; }
    public float height { get; init; }
    public float x { get; init; }
    public float y { get; init; }
    public SpriteEffects effect { get; init; }
    public float rotation { get; init; }
    public float scale { get; init; }
}

public abstract class BasicTouhouPet : ModProjectile
{
    // The live pets, by projectile slot: each one writes itself here in PreAI.
    static readonly BasicTouhouPet?[] live = new BasicTouhouPet?[Main.maxProjectiles + 1];

    sealed class ChatRow
    {
        public string text = "";
        public float w, h;
        public string[] chars = Array.Empty<string>();
        public float[] offsets = Array.Empty<float>();
    }

    // The owner, read when used: the draw hooks can run before the first PreAI.
    protected Player player => Main.player[Projectile.owner];
    int owner;
    uint seen;

    // The pet's chat color; each pet sets its own.
    protected Color myColor;
    protected virtual Color InitialColor => new Color(255, 255, 255);

    protected int mainTimer;
    protected int[] extraAI = new int[3];
    public int PetState;
    public int ChatIndex;
    public int ChatTimeLeft;
    public int ChatCD;
    protected int talkInterval;
    protected bool chatFuncIsOccupied;
    protected bool textShaking;
    float chatAlpha;
    float chatScale = 1f;
    float chatBaseY;
    Color chatColor;
    string? chatText;
    int chatLag;
    int textLength;
    string? wrappedText;
    SpriteFont wrappedFont;
    ChatRow[] wrappedRows = Array.Empty<ChatRow>();
    int wrappedCount;

    public override void SetDefaults()
    {
        Projectile.netImportant = true;
        Projectile.width = 50;
        Projectile.height = 50;
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.ignoreWater = true;

        myColor = InitialColor;
        chatColor = new Color(255, 255, 255);
    }

    // ---------------------------------------------------------------- frame

    public override bool PreAI()
    {
        owner = Projectile.owner;
        seen = Main.GameUpdateCount;
        live[Projectile.whoAmI] = this;

        if (mainTimer == 0)
        {
            ChatIndex = 0;
            talkInterval = 0;
            ChatCD = 0;
            chatFuncIsOccupied = false;
            Array.Clear(extraAI);
            mainTimer = 1;
        }
        if (++mainTimer >= 4800)
            mainTimer = 1;
        UpdateChat();
        return true;
    }

    public override void PostAI() => VisualEffectForPreview();

    public override bool? CanCutTiles() => false;

    public override bool? CanDamage() => false;

    public override void PostDraw(Player player, Color lightColor)
    {
        if (chatAlpha > 0 && Projectile.owner == Main.myPlayer)
        {
            Vector2 pos = Projectile.position;
            Vector2 screen = Main.screenPosition;
            DrawChatPanel(pos.X - screen.X + Projectile.width / 2f, pos.Y - screen.Y - 20, chatAlpha * Projectile.Opacity);
        }
    }

    // Animation that also runs on the character preview (frames of wings, clothes...).
    protected virtual void VisualEffectForPreview() { }

    // The pet's lines.
    protected virtual List<ChatLine> ChatLines(Player player) => new();

    // ----------------------------------------------------------------- text

    // A line of this pet: Chat.<Pet>.<key>.
    protected string Text(string key) => Language.GetTextValue("Mods.touhoupets.Chat." + GetType().Name + "." + key) ?? key;

    void UpdateChat()
    {
        if (chatLag > 0)
            chatLag--;
        else
            chatLag = 0;

        if (ChatTimeLeft > 0)
        {
            if (chatLag <= 0)
            {
                chatBaseY += 1.2f;
                chatScale += 0.05f;
                chatAlpha += 0.1f;
                ChatTimeLeft--;
            }
        }
        else
        {
            textLength = 0;
            chatBaseY = 0;
            chatScale = 1;
            ChatTimeLeft = 0;
            chatAlpha -= 0.05f;
            if (talkInterval > 0)
                talkInterval--;
            if (ChatCD > 0)
                ChatCD--;
        }
        if (chatBaseY >= 0)
            chatBaseY = 0;
        if (chatScale >= 1)
            chatScale = 1;
        chatAlpha = Math.Min(Math.Max(chatAlpha, 0), 1);

        if (chatFuncIsOccupied)
        {
            ChatIndex = 0;
            ChatTimeLeft = 0;
            chatText = null;
            talkInterval = 0;
            ChatCD = 0;
        }

        if (mainTimer % 30 == 0)
            textLength++;
    }

    // A Chinese character counts one, a letter about a third (see the top).
    static int Units(string text)
    {
        float units = 0;
        foreach (Rune ch in text.EnumerateRunes())
            units += ch.Value >= 0x2E80 ? 1f : 0.35f;
        return Math.Max(1, (int)MathF.Ceiling(units));
    }

    // Picks one of the pet's lines by weight (GetChatAndSetIndex).
    ChatLine? PickChat()
    {
        List<ChatLine> lines = ChatLines(player);
        if (lines.Count == 0)
            return null;
        float total = 0;
        foreach (ChatLine line in lines)
            total += line.Weight;
        float roll = Main.rand.NextFloat() * total;
        foreach (ChatLine line in lines)
        {
            roll -= line.Weight;
            if (roll < 0)
                return line;
        }
        return lines[lines.Count - 1];
    }

    // Says something, unless the pet is talking or in its cooldown. Without
    // altText, one of its own lines (ChatLines).
    protected void SetChat(Color? color = null, string? altText = null, int altIndex = 0, int lag = 0, int timeLeftPreWord = 20, bool breakTimeLimit = false)
    {
        if (ChatTimeLeft > 0 || ChatCD > 0)
            return;
        Color chatLineColor;
        if (color.HasValue)
        {
            chatLineColor = color.Value;
        }
        else
        {
            Vector2 c = Projectile.Center;
            chatLineColor = Lighting.GetColor((int)MathF.Floor(c.X / 16), (int)MathF.Floor(c.Y / 16));
        }

        string? chat = altText;
        int index = altIndex;
        if (chat == null)
        {
            if (PickChat() is not ChatLine line)
                return;
            index = line.Index;
            chat = line.Key == null ? "" : Text(line.Key);
        }
        if (chat.Length == 0)
            return;

        int units = Units(chat);
        if (units > 10 && breakTimeLimit && timeLeftPreWord > 10)
            timeLeftPreWord = 10;
        ChatIndex = index;
        chatBaseY = -24;
        chatScale = 0;
        chatText = chat;
        wrappedText = null;
        ChatTimeLeft = units * timeLeftPreWord;
        chatColor = chatLineColor;
        chatLag = lag;
    }

    // A line in a conversation: sets this pet's cooldown and the other's
    // (cd2, or cd), so neither drifts into another line in the middle.
    protected void SetChatWithOtherOne(BasicTouhouPet? other, string text, Color color, int index, int cd, int cd2 = -1, int timeleft = 20, bool breakLimit = false)
    {
        if (ChatCD > 0)
            ChatCD = 0;
        SetChat(color, text, index, 20, timeleft, breakLimit);
        ChatCD = cd;
        if (other != null)
            other.ChatCD = cd2 == -1 ? cd : cd2;
    }

    // ------------------------------------------------------------- lookups

    // Whether this slot holds a live pet of this owner; forgets the dead ones.
    bool IsLivePet(int slot, out BasicTouhouPet? pet)
    {
        pet = live[slot];
        if (pet == null)
            return false;
        if (pet.seen + 1 < seen)
        {
            live[slot] = null;
            pet = null;
            return false;
        }
        return pet.owner == owner;
    }

    // A pet of the class T saying line minIndex..maxIndex, with at most
    // timeLeft frames of it left (0: any), and out of its cooldown unless
    // ignoreCD (FindChatIndex). null when there is none.
    protected T? FindChatIndex<T>(int minIndex, int maxIndex = 0, int timeLeft = 1, bool ignoreCD = false) where T : BasicTouhouPet
    {
        if (maxIndex <= minIndex)
            maxIndex = minIndex;
        T? target = null;
        for (int slot = 0; slot < live.Length; slot++)
        {
            if (!IsLivePet(slot, out BasicTouhouPet? pet) || pet is not T p)
                continue;
            if (p.ChatCD > 0 && !ignoreCD)
                continue;
            if (p.ChatIndex < minIndex || p.ChatIndex > maxIndex)
                continue;
            if (timeLeft > 0 && p.ChatTimeLeft > timeLeft)
                continue;
            target = p;
        }
        return target;
    }

    // This pet's own line chained to the next one (FindChainedChat).
    protected bool FindChainedChat(int minIndex, int maxIndex = 0, int timeLeft = 1, float alphaLeft = 0, bool ignoreCD = true)
    {
        if (maxIndex <= minIndex)
            maxIndex = minIndex;
        if (ChatCD > 0 && !ignoreCD)
            return false;
        if (ChatIndex < minIndex || ChatIndex > maxIndex)
            return false;
        if (timeLeft > 0 && ChatTimeLeft > timeLeft)
            return false;
        return chatAlpha <= alphaLeft;
    }

    // A pet of the class T in state minState..maxState (FindPetState).
    protected T? FindPetState<T>(int minState, int maxState = 0) where T : BasicTouhouPet
    {
        if (maxState <= minState)
            maxState = minState;
        T? target = null;
        for (int slot = 0; slot < live.Length; slot++)
        {
            if (!IsLivePet(slot, out BasicTouhouPet? pet) || pet is not T p)
                continue;
            if (p.PetState >= minState && p.PetState <= maxState)
                target = p;
        }
        return target;
    }

    // Whether the owner has a pet of the class T.
    protected bool Owns<T>() where T : ModProjectile =>
        player.ownedProjectileCounts[ModContent.ProjectileType<T>()] > 0;

    // ------------------------------------------------------------- movement

    // Flies to the player's center plus (x, y) (MoveToPoint).
    protected void MoveToPoint(float x, float y, float speed)
    {
        Vector2 mounted = player.MountedCenter;
        float tx = mounted.X + x;
        float ty = mounted.Y + y;
        Vector2 c = Projectile.Center;
        float dist = Vector2.Distance(new Vector2(tx, ty), c);
        if (dist > 1200)
        {
            Vector2 pc = player.Center;
            Projectile.Center = new Vector2(pc.X + x, pc.Y + y);
            c = Projectile.Center;
        }
        float vx = tx - c.X;
        float vy = ty - c.Y;
        if (dist < 1)
            Projectile.velocity *= 0.25f;
        if (vx != 0 || vy != 0)
        {
            if (new Vector2(vx, vy).Length() < 1)
                Projectile.velocity = new Vector2(vx, vy);
            else
                Projectile.velocity = new Vector2(vx * 0.01f * speed, vy * 0.01f * speed);
        }
    }

    // Faces where it flies; style2 (a pet behind the player) faces the
    // player's way when close (ChangeDir).
    protected void ChangeDir(Player player, bool style2 = false, float dist = 100)
    {
        float vx = Projectile.velocity.X;
        if (style2)
        {
            if (Vector2.Distance(Projectile.Center, player.Center) <= dist)
                Projectile.spriteDirection = player.direction;
            else if (vx > 0.25f)
                Projectile.spriteDirection = 1;
            else if (vx < -0.25f)
                Projectile.spriteDirection = -1;
        }
        else if (vx > 0.25f)
        {
            Projectile.spriteDirection = 1;
        }
        else if (vx < -0.25f)
        {
            Projectile.spriteDirection = -1;
        }
        else
        {
            Projectile.spriteDirection = player.direction;
        }
    }

    // ------------------------------------------------------------- drawing

    protected PetDrawInfo DrawInfo()
    {
        Texture2D t = TextureAssets.Projectile[Projectile.type].Value;
        Vector2 c = Projectile.Center;
        Vector2 screen = Main.screenPosition;
        return new PetDrawInfo
        {
            t = t,
            width = t.Width,
            height = t.Height / (float)Main.projFrames[Projectile.type],
            x = c.X - screen.X,
            y = c.Y - screen.Y + 7 * Main.essScale,
            effect = Projectile.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally,
            rotation = Projectile.rotation,
            scale = Projectile.scale,
        };
    }

    // One frame of the pet's sheet: column `col` of `cols`, row `frame`,
    // moved by (dx, dy). The origin is the frame's center.
    protected void DrawFrame(in PetDrawInfo d, int frame, Color color, int cols = 1, int col = 0, float dx = 0, float dy = 0, SpriteEffects? effect = null)
    {
        int w = (int)MathF.Floor(d.width / (float)cols);
        ModUtils.Draw(d.t, new Vector2(d.x + dx, d.y + dy), new Rectangle(col * w, (int)(frame * d.height), w, (int)d.height),
            color, d.rotation, new Vector2(w / 2f, d.height / 2f), d.scale, effect ?? d.effect);
    }

    void WrapChat(string text)
    {
        if (wrappedText == text)
            return;
        SpriteFont font = FontAssets.MouseText.Value;
        var array = Utils.WordwrapString(text, font, 240, 10, out int count);
        var rows = new List<ChatRow>();
        for (int i = 0; i <= count && i < array.Length; i++)
        {
            string? line = array[i];
            if (line == null)
                continue;
            Vector2 size = font.MeasureString(line);
            // Where each character starts, for the shaking text.
            var chars = new List<string>();
            foreach (Rune ch in line.EnumerateRunes())
                chars.Add(ch.ToString());
            var offsets = new float[chars.Count];
            var prefix = new StringBuilder();
            for (int k = 0; k < chars.Count; k++)
            {
                offsets[k] = font.MeasureString(prefix.ToString()).X;
                prefix.Append(chars[k]);
            }
            rows.Add(new ChatRow { text = line, w = size.X, h = size.Y, chars = chars.ToArray(), offsets = offsets });
        }
        wrappedText = text;
        wrappedFont = font;
        wrappedRows = rows.ToArray();
        wrappedCount = count + 1;
    }

    void DrawChatPanel(float x, float y, float alpha)
    {
        string? text = chatText;
        if (string.IsNullOrWhiteSpace(text))
            return;
        const float total = 0.9f;
        WrapChat(text);
        SpriteBatch sb = Main.spriteBatch;
        Color color = Color.Multiply(chatColor, alpha);
        Color border = Color.Multiply(Color.Black, alpha);
        int n = wrappedCount;
        float revealed = textLength;

        for (int i = 0; i < wrappedRows.Length; i++)
        {
            ChatRow line = wrappedRows[i];
            float ox = line.w * 0.5f;
            float oy = line.h * 0.5f;
            var origin = new Vector2(ox, oy);
            float ly = y + oy + i * 30 * total - (n - 1) * 30 * total;
            if (textShaking)
            {
                // Koishi: the line comes in one character at a time, shaking.
                for (int k = 0; k < line.chars.Length; k++)
                {
                    string ch = line.chars[k];
                    revealed -= char.ConvertToUtf32(ch, 0) >= 0x2E80 ? 1f : 0.35f;
                    if (revealed < 0)
                        return;
                    Utils.DrawBorderStringFourWay(sb, wrappedFont, ch, x + ox / 8 + line.offsets[k] + Main.rand.Next(-2, 2), ly + Main.rand.Next(-2, 2),
                        color, border, origin, chatScale * total);
                }
            }
            else
            {
                Utils.DrawBorderStringFourWay(sb, wrappedFont, line.text, x + ox / 8, ly - chatBaseY, color, border, origin, chatScale * total);
            }
        }
    }
}
