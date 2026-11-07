// SPDX-License-Identifier: GPL-3.0-only
// Modified on 2026-11-06: ported from JavaScript to C#.
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using static WildAndHornedMod.Common.Pets.Runtime;

namespace WildAndHornedMod.Common.Pets;

public abstract class PetItem : ModItem
{
    protected abstract PetDefinition Definition { get; }
    protected abstract int PetType { get; }
    protected abstract int PetBuffType { get; }

    public override void SetDefaults()
    {
        PetDefinition d = Definition;
        Item.DefaultToVanitypet(PetType, PetBuffType);
        Item.width = d.Size.Width;
        Item.height = d.Size.Height;
        Item.rare = ItemRarityID.Orange;
        Item.value = 20000;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        player.AddBuff(Item.buffType, 2, false);
        return false;
    }

    public override void AddRecipes()
    {
        foreach (PetRecipe data in Definition.Recipes)
        {
            Recipe recipe = CreateRecipe();
            foreach (PetIngredient ingredient in data.Ingredients)
            {
                if (ingredient.Group != null)
                    recipe.AddRecipeGroup(ingredient.Group, ingredient.Amount);
                else
                    recipe.AddIngredient(ingredient.Item, ingredient.Amount);
            }
            recipe.AddTile(data.Tile).Register();
        }
    }
}

public abstract class PetBuff : ModBuff
{
    protected abstract PetDefinition Definition { get; }
    protected abstract int PetType { get; }

    public override void SetStaticDefaults()
    {
        Main.buffNoTimeDisplay[Type] = true;
        Main.vanityPet[Type] = !Definition.Light;
        Main.lightPet[Type] = Definition.Light;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        bool unused = false;
        player.BuffHandle_SpawnPetIfNeededAndSetTime(buffIndex, ref unused, PetType, 18000);
    }
}

public abstract class PetBase : ModProjectile
{
    public abstract PetDefinition Definition { get; }
    protected abstract int PetBuffType { get; }

    Asset<Texture2D>? sheet;
    public Asset<Texture2D> Sheet => sheet ??= ModContent.Request<Texture2D>("wildandhorned/Content/Pets/" + Definition.Name);

    public Player Owner => Main.player[Projectile.owner];
    public bool IsOwner => Projectile.owner == Main.myPlayer;
    public int PetState
    {
        get => (int)Projectile.ai[1];
        set
        {
            if (!IsOwner || PetState == value)
                return;
            Projectile.ai[1] = value;
            Projectile.netUpdate = true;
        }
    }
    public virtual string CurrentAction => "";
    public virtual bool IsTransforming => false;
    public bool CanTalk => PetState <= 2 && !IsTransforming;

    protected int mainTimer, timer, cooldown, lastState;
    protected int step, stepCounter;
    public int followSpot, lastFrame;
    public int alert, aura, glow;
    public bool enemyNear, treasureNear;
    public string chatKey = "";
    protected string replyKey = "";
    public int chatLag, chatLife, chatDuration;
    float lastChatCode;
    int replyIndex = -1;
    int replyType;
    public string DisplayedForm = "Hermit";

    // The chat line as drawn, kept while the key stays the same (see Runtime.DrawChat).
    public string? wrappedKey;
    public string[] wrappedLines = Array.Empty<string>();
    public Vector2[] wrappedOrigins = Array.Empty<Vector2>();

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = Definition.Frames;
        Main.projPet[Type] = true;
        ProjectileID.Sets.LightPet[Type] = Definition.Light;
    }

    public override void SetDefaults()
    {
        Projectile.width = 50;
        Projectile.height = 50;
        Projectile.netImportant = true;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.damage = 0;
        Projectile.penetrate = -1;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 5;
        Projectile.aiStyle = -1;
    }

    public override bool PreAI()
    {
        Player owner = Owner;
        if (!owner.active)
        {
            Projectile.active = false;
            return false;
        }
        if (!owner.dead && owner.FindBuffIndex(PetBuffType) >= 0)
            Projectile.timeLeft = 2;
        mainTimer = (mainTimer + 1) % 4801;
        UpdateChat();
        return true;
    }

    protected void Move() => MovePet(this);

    public override bool? CanDamage() => false;

    public override bool? CanCutTiles() => false;

    public override void PostDraw(Player player, Color lightColor) => DrawChat(this);

    public void Speak(int line, int lag = 20)
    {
        if (!IsOwner || Main.dedServ)
            return;
        if (line <= 0 || line >= 1000)
            return;
        SendChat(line, lag);
        foreach (PetCompanion conversation in Definition.Companions)
        {
            if (conversation.Line != line)
                continue;
            PetBase? partner = Companion(this, conversation.Pet, conversation.Form);
            if (partner != null && partner.CanTalk && partner.chatLife == 0)
            {
                replyKey = Definition.Name + line;
                replyIndex = partner.Projectile.whoAmI;
                replyType = partner.Projectile.type;
            }
            break;
        }
    }

    public void SpeakReply(string key, int lag = 20)
    {
        if (!IsOwner || Main.dedServ)
            return;
        int replyId = Array.IndexOf(replyKeys, key);
        if (replyId < 0)
            return;
        int code = 100 + replyId;
        if (code >= 1000)
            return;
        SendChat(code, lag);
    }

    void SendChat(int code, int lag)
    {
        // Keep a sequence in ai[2] so repeated lines also reach remote clients.
        int sequence = ((int)MathF.Floor(Projectile.ai[2] / 1000) + 1) % 16000;
        Projectile.ai[2] = sequence * 1000 + code;
        Projectile.netUpdate = true;
        ReceiveChat(Projectile.ai[2], lag);
    }

    void ReceiveChat(float value, int lag = 20)
    {
        lastChatCode = value;
        int code = (int)value % 1000;
        if (code >= 100 && code - 100 < replyKeys.Length)
            chatKey = "Replies." + replyKeys[code - 100];
        else if (code > 0 && code < 100)
            chatKey = "Chat_" + Definition.Name + ".Chat" + code;
        else
            return;
        string text = Translate(chatKey) ?? "";
        chatDuration = Math.Max(150, Math.Min(420, text.Length * 3));
        chatLife = chatDuration;
        chatLag = lag;
        wrappedKey = null;
    }

    void UpdateChat()
    {
        if (Projectile.ai[2] != lastChatCode)
            ReceiveChat(Projectile.ai[2]);
        if (chatLag > 0)
            chatLag--;
        else if (chatLife > 0)
            chatLife--;
        if (!IsOwner)
            return;
        if (chatLife == 0 && replyKey.Length > 0)
        {
            if (replyIndex >= 0)
            {
                Projectile target = Main.projectile[replyIndex];
                if (target.active && target.type == replyType && target.owner == Projectile.owner &&
                    Vector2.DistanceSquared(Projectile.Center, target.Center) < 1200f * 1200f)
                {
                    if (target.ModProjectile is PetBase partner && partner.CanTalk && partner.chatLife == 0)
                        partner.SpeakReply(replyKey, 15);
                }
            }
            replyKey = "";
            replyIndex = -1;
            replyType = 0;
        }
        if (CanTalk && chatLife == 0 && replyKey.Length == 0 && mainTimer % 900 == 0 && Chance(6) && !BossNear(this))
        {
            List<(int Line, float Weight)> lines = RegularLines();
            if (lines.Count > 0)
            {
                var weights = new float[lines.Count];
                for (int i = 0; i < weights.Length; i++)
                    weights[i] = lines[i].Weight;
                Speak(lines[Logic.WeightedIndex(weights, Main.rand.NextFloat())].Line);
            }
        }
    }

    protected virtual List<(int Line, float Weight)> RegularLines()
    {
        var lines = new List<(int Line, float Weight)>();
        foreach (int line in Definition.Regular)
            lines.Add((line, 1));
        foreach (PetCompanion c in Definition.Companions)
        {
            if (Companion(this, c.Pet, c.Form) != null)
                lines.Add((c.Line, 2));
        }
        return lines;
    }
}
