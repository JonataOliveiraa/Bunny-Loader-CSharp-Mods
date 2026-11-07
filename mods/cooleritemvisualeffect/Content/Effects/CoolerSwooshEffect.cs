using System;
using BunnyLoader;
using CoolerItemVisualEffectMod.Common.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace CoolerItemVisualEffectMod.Content.Effects;

// Draws the swing's arc (or the stab's spike) on the screen from what CoolerItemFx holds.
public class CoolerSwooshEffect : ModScreenEffect
{
    public const string EffectKey = "cooleritem:swoosh";

    const string ShaderGlsl = """
#include <bl/screen>

uniform vec2  uCICenter;
uniform float uCIHeadMetric;
uniform vec2  uCIBladeDir;
uniform float uCISpan;
uniform float uCILen;
uniform float uCIInvLen;
uniform float uCIVerticalStretch;
uniform float uCIXStretch;
uniform float uCIChroma;
uniform float uCIGrow;
uniform float uCITime;
uniform float uCIAlpha;
uniform float uCIStrength;
uniform float uCIDistort;
uniform float uCIHalo;
uniform float uCIAR;
uniform float uCIAG;
uniform float uCIAB;
uniform float uCIBR;
uniform float uCIBG;
uniform float uCIBB;
uniform float uCICR;
uniform float uCICG;
uniform float uCICB;
uniform float uCIPrismatic;
uniform float uCISprite;
uniform float uCIStyle;
uniform float uCIProgress;
uniform float uCIStab;
uniform float uCICut;
uniform float uCIRapidStab;
uniform float uCISqueeze;

const float PI = 3.14159265;
const float TAU = 6.28318531;

float gTravel = 0.0;
float gRad = 0.0;
float gAlong = 0.0;
float gSide = 0.0;

float wrapPi(float a) { return a - TAU * floor((a + PI) / TAU); }

float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
float vnoise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), f.x),
               mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), f.x), f.y);
}

float slashBandAt(float r, float tt, float progress, out float edgeValue) {
    edgeValue = 0.0;
    if (tt < 0.0 || tt > 1.0) return 0.0;
    float turbulence = (abs(uCIStyle - 2.0) < 0.45) ? 1.45 : 1.0;
    float edgeNoise = (0.5 * sin(tt * 31.0 + uCITime * 0.72) + 0.22 * sin(tt * 59.0 - uCITime * 1.10)) * turbulence;
    float outer = 1.04 * (1.0 + 0.10 * uCIGrow * tt + edgeNoise * (0.004 + 0.008 * tt));
    float innerHead = mix(0.46, 0.08, smoothstep(0.02, 0.32, progress));
    if (uCICut > 0.5) innerHead = max(innerHead, 0.50);
    float innerR = mix(innerHead, 0.66, pow(tt, 0.72));
    float inner = min(innerR, outer - 0.06);
    edgeValue = (r - inner) / max(outer - inner, 0.001);
    if (edgeValue < 0.0 || edgeValue > 1.0) return 0.0;
    float body = smoothstep(0.0, 0.22, edgeValue) * (1.0 - smoothstep(0.93, 1.0, edgeValue));
    float frontCap = smoothstep(0.0, 0.025, tt);
    float tailFade = pow(max(0.0, 1.0 - tt), 0.95);
    return body * frontCap * tailFade;
}

float shapeAt(vec2 uv, float aspect, out float t, out float e) {
    t = 0.0;
    e = 0.0;
    vec2 d = (uv - uCICenter) * vec2(aspect, 1.0);

    if (uCIStab > 0.5) {
        vec2 forward = uCIBladeDir;
        vec2 sideDir = vec2(-forward.y, forward.x);
        float prog = clamp(uCIProgress, 0.0, 1.0);
        float extend = (uCIRapidStab > 0.5)
            ? abs(sin(PI * 3.0 * prog))
            : smoothstep(0.0, 0.40, prog) * (1.0 - smoothstep(0.62, 1.0, prog));
        float tipAlong = 1.0 + 0.50 * extend;
        float along = dot(d, forward) * uCIInvLen;
        float side = abs(dot(d, sideDir)) * uCIInvLen;
        gAlong = along;
        gSide = side;
        float a = along / tipAlong;
        if (a < 0.0 || a > 1.0) { t = 1.0; e = 0.0; return 0.0; }
        float maxHW = 0.085 * (0.35 + 0.65 * extend);
        float hw = max(maxHW * pow(sin(PI * pow(a, 0.62)), 0.85), 0.0015);
        float across = side / hw;
        t = 1.0 - a;
        e = clamp(1.0 - across, 0.0, 1.0);
        if (across > 1.0) return 0.0;
        float tailIn = smoothstep(0.0, 0.14, a);
        float tipOut = 1.0 - smoothstep(0.94, 1.0, a);
        return (1.0 - smoothstep(0.55, 1.0, across)) * tailIn * tipOut * (0.35 + 0.65 * extend);
    }

    d.x /= uCIXStretch;
    d.y /= uCIVerticalStretch;
    float spanAbs = abs(uCISpan);
    if (spanAbs < 0.035) return 0.0;
    float rawR = length(d);
    gRad = rawR;
    float r = rawR * uCIInvLen;
    float rel = wrapPi(atan(d.y, d.x) - uCIHeadMetric);
    float travel;
    if (uCISpan < 0.0) {
        if (rel > 0.0) rel -= TAU;
        travel = -rel;
    } else {
        if (rel < 0.0) rel += TAU;
        travel = rel;
    }
    gTravel = travel;

    if (spanAbs < TAU - 0.001 && travel > spanAbs) return 0.0;
    float progress = clamp(uCIProgress, 0.0, 1.0);
    float primaryT = clamp(travel / spanAbs, 0.0, 1.0);
    float edgePrimary;
    float maskPrimary = slashBandAt(r, primaryT, progress, edgePrimary);
    t = primaryT;
    e = edgePrimary;
    float bestMask = maskPrimary;

    float extraSpan = max(0.0, spanAbs - TAU);
    if (extraSpan > 0.001 && travel < extraSpan) {
        float tailT = (travel + TAU) / spanAbs;
        float edgeTail;
        float maskTail = slashBandAt(r, tailT, progress, edgeTail);
        if (maskTail > bestMask) {
            bestMask = maskTail;
            t = tailT;
            e = edgeTail;
        }
    }
    return bestMask;
}

vec3 hsv2rgb(vec3 c) {
    vec3 p = abs(fract(c.xxx + vec3(0.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0);
    return c.z * mix(vec3(1.0), clamp(p - 1.0, 0.0, 1.0), c.y);
}

void main() {
    float aspect = uAspect > 0.01 ? uAspect : (uResolution.x / max(uResolution.y, 1.0));
    vec2 uvC = clamp(vUV, vec2(0.001), vec2(0.999));
    bool stab = uCIStab > 0.5;

    float tx = max(uScreenTexel.x * aspect, uScreenTexel.y);
    vec2 dC = (vUV - uCICenter) * vec2(aspect, 1.0);
    float reachLen = stab ? 1.52 : 1.04 * (1.015 + 0.10 * max(uCIGrow, 0.0));
    float stretchMax = stab ? 1.0 : max(uCIXStretch, uCIVerticalStretch);
    float reach = uCILen * reachLen * stretchMax + 16.0 * tx;
    if ((!stab && abs(uCISpan) < 0.035) || dot(dC, dC) > reach * reach) {
        fragColor = vec4(blScreen(uvC).rgb, 1.0);
        return;
    }

    float t, e;
    float M = shapeAt(vUV, aspect, t, e);

    bool nearArc = true;
    if (M <= 0.0) {
        if (stab) {
            float m = 15.0 * tx * uCIInvLen;
            nearArc = gAlong > -m && gAlong < 1.5 + m && gSide < 0.085 + m;
        } else {
            float mm = 15.0 * tx / min(min(uCIXStretch, uCIVerticalStretch), 1.0);
            float outerMax = uCILen * 1.04 * (1.015 + 0.10 * max(uCIGrow, 0.0));
            nearArc = gRad < outerMax + mm && gRad > uCILen * 0.07 - mm;
            if (nearArc && abs(uCISpan) < TAU - 0.001 && gRad > mm) {
                float am = asin(min(1.0, mm / gRad));
                nearArc = gTravel <= abs(uCISpan) + am || gTravel >= TAU - am;
            }
        }
    }

    float H = 0.0;
    if (M > 0.0 || nearArc) {
        float t2, e2;
        vec2 o1 = uScreenTexel * 6.0;
        vec2 o2 = uScreenTexel * 13.0;
        H = shapeAt(vUV + vec2(o1.x, 0.0), aspect, t2, e2) + shapeAt(vUV - vec2(o1.x, 0.0), aspect, t2, e2) +
            shapeAt(vUV + vec2(0.0, o1.y), aspect, t2, e2) + shapeAt(vUV - vec2(0.0, o1.y), aspect, t2, e2) +
            shapeAt(vUV + vec2(o2.x, 0.0), aspect, t2, e2) * 0.42 + shapeAt(vUV - vec2(o2.x, 0.0), aspect, t2, e2) * 0.42 +
            shapeAt(vUV + vec2(0.0, o2.y), aspect, t2, e2) * 0.42 + shapeAt(vUV - vec2(0.0, o2.y), aspect, t2, e2) * 0.42;
        H *= 0.14;
    }

    if (M <= 0.0 && H <= 0.0 && uCISqueeze <= 0.001) {
        fragColor = vec4(blScreen(uvC).rgb, 1.0);
        return;
    }

    vec3 spriteCol = vec3(0.0);
    float spriteW = 0.0;
    if (M > 0.01 && uCISprite > 0.5) {
        vec2 xyS = stab ? vec2(1.0) : vec2(uCIXStretch, uCIVerticalStretch);
        vec2 dq = dC / xyS;
        float rr = clamp(length(dq) * uCIInvLen, 0.22, 0.97);
        vec2 bdir = vec2(uCIBladeDir.x / aspect, uCIBladeDir.y);
        vec2 bside = normalize(vec2(-uCIBladeDir.y / aspect, uCIBladeDir.x)) * uScreenTexel * 3.0;
        vec2 pc = uCICenter + bdir * (rr * uCILen);
        float bestScore = 0.0;
        for (int k = -2; k <= 2; k++) {
            vec3 s = blScreen(clamp(pc + bside * float(k) * 0.5, vec2(0.001), vec2(0.999))).rgb;
            float mx = max(s.r, max(s.g, s.b));
            float mn = min(s.r, min(s.g, s.b));
            float sc = (mx - mn) + 0.65 * mx - 0.12;
            if (sc > bestScore) { bestScore = sc; spriteCol = s; }
        }
        spriteW = smoothstep(0.30, 0.55, bestScore);
    }

    float streak = 0.0, innerRim = 0.0, outerRim = 0.0, I = 0.0;
    if (M > 0.0) {
        float n1 = vnoise(vec2(t * 16.0 + uCITime * 0.19, e * 4.0 - uCITime * 0.60));
        float n2 = vnoise(vec2(e * 17.0 - uCITime * 0.44, t * 8.0 + uCITime * 0.13));
        float waveRate = (abs(uCIStyle - 2.0) < 0.45) ? 5.2 : 3.8;
        float flowWave = 0.5 + 0.5 * sin(t * 31.0 - uCITime * waveRate + (n1 - 0.5) * 5.0 + (n2 - 0.5) * 2.0);
        streak = smoothstep(0.70, 0.96, flowWave) * (1.0 - smoothstep(0.48, 0.96, t)) * M;
        streak *= 0.24 + 0.42 * n2;
        innerRim = exp(-pow((e - 0.105) / 0.052, 2.0)) * M;
        outerRim = exp(-pow((e - 0.855) / 0.075, 2.0)) * M;
        I = M * (0.48 + 0.82 * n1) + streak * 0.72 + innerRim * 0.10 + outerRim * 0.20;
    }

    vec2 xyScale = stab ? vec2(1.0) : vec2(uCIXStretch, uCIVerticalStretch);
    vec2 dd = dC / xyScale;
    float lens = 0.25 + 0.75 * smoothstep(0.0, 0.55, e);
    float edgeDistort = M * lens + outerRim * 0.45 + min(H, 1.0) * 0.40;
    float warp = stab ? 0.0 : uCIDistort * 1.4 * uCIAlpha;
    vec2 off = vec2(0.0);
    if (warp > 0.0 && edgeDistort > 0.0) {
        vec2 nrm = dd / max(length(dd), 0.0001);
        vec2 nrmS = normalize(nrm / xyScale + vec2(0.00001));
        off = (nrmS * edgeDistort) * vec2(1.0 / aspect, 1.0) * warp;
    }
    vec2 sampleUV = vUV + off;
    float stabWarp = 0.0;
    vec2 stabForward = uCIBladeDir;
    if (uCISqueeze > 0.001 && stab) {
        vec2 stabSide = vec2(-stabForward.y, stabForward.x);
        float alongDist = dot(dd, stabForward) * uCIInvLen;
        float sideDist = dot(dd, stabSide) * uCIInvLen;
        float outputHalfWidth = 0.050 + 0.055 * clamp(alongDist, 0.0, 1.0);
        float originalHalfWidth = 0.22 + 0.06 * clamp(alongDist, 0.0, 1.0);
        float alongMask = smoothstep(-0.14, -0.015, alongDist) *
                          (1.0 - smoothstep(1.02, 1.17, alongDist));
        float sideAbs = abs(sideDist);
        float squeezeFactor = mix(1.0, 0.24, clamp(uCISqueeze, 0.0, 1.0));

        float innerMask = 1.0 - smoothstep(outputHalfWidth * 0.74, outputHalfWidth, sideAbs);
        float sourceSide = sign(sideDist) * min(abs(sideDist / max(squeezeFactor, 0.16)), originalHalfWidth);
        float sourceAlong = alongDist - 0.12 * clamp(uCISqueeze, 0.0, 1.0);
        vec2 sourceD = stabForward * (sourceAlong * uCILen) + stabSide * (sourceSide * uCILen);
        vec2 warpedUV = uCICenter + vec2(sourceD.x / aspect, sourceD.y) + off;

        float outerBand = smoothstep(outputHalfWidth * 0.78, outputHalfWidth * 1.06, sideAbs) *
                          (1.0 - smoothstep(originalHalfWidth * 0.78, originalHalfWidth, sideAbs));
        float eraseSide = sign(sideDist) * (originalHalfWidth + 0.045);
        vec2 eraseD = stabForward * (sourceAlong * uCILen) + stabSide * (eraseSide * uCILen);
        vec2 eraseUV = uCICenter + vec2(eraseD.x / aspect, eraseD.y) + off;

        float strength = clamp(uCISqueeze, 0.0, 1.0) * alongMask;
        float innerWarp = strength * innerMask;
        float outerErase = strength * outerBand;
        sampleUV = mix(sampleUV, warpedUV, innerWarp);
        sampleUV = mix(sampleUV, eraseUV, outerErase);
        stabWarp = max(innerWarp, outerErase);
    }
    sampleUV = clamp(sampleUV, vec2(0.001), vec2(0.999));

    vec3 src = blScreen(sampleUV).rgb;
    if (uCIChroma > 0.001 && !stab && (off.x != 0.0 || off.y != 0.0)) {
        vec2 caOff = off * uCIChroma;
        src.r = blScreen(clamp(sampleUV + caOff, vec2(0.001), vec2(0.999))).r;
        src.b = blScreen(clamp(sampleUV - caOff, vec2(0.001), vec2(0.999))).b;
    }
    if (stabWarp > 0.001) {
        vec2 axisUV = normalize(vec2(stabForward.x / aspect, stabForward.y));
        vec2 blurOff = axisUV * uScreenTexel * 3.2;
        vec3 blurA = blScreen(clamp(sampleUV + blurOff, vec2(0.001), vec2(0.999))).rgb;
        vec3 blurB = blScreen(clamp(sampleUV - blurOff, vec2(0.001), vec2(0.999))).rgb;
        src = mix(src, src * 0.52 + blurA * 0.29 + blurB * 0.19, stabWarp * 0.50);
        vec2 chromaOff = axisUV * uScreenTexel * (1.3 + 2.5 * stabWarp);
        float red = blScreen(clamp(sampleUV + chromaOff, vec2(0.001), vec2(0.999))).r;
        float blue = blScreen(clamp(sampleUV - chromaOff, vec2(0.001), vec2(0.999))).b;
        src = mix(src, vec3(red, src.g, blue), stabWarp * 0.72);
    }

    vec3 c0 = mix(vec3(uCIAR, uCIAG, uCIAB), vec3(uCIBR, uCIBG, uCIBB), 0.45);
    vec3 c1 = vec3(uCIBR, uCIBG, uCIBB);
    vec3 c2 = vec3(uCICR, uCICG, uCICB);
    if (spriteW > 0.0) {
        float lum = dot(spriteCol, vec3(0.299, 0.587, 0.114));
        vec3 sc1 = clamp(mix(vec3(lum), spriteCol, 1.35), 0.0, 1.0);
        sc1 = sc1 / max(max(sc1.r, max(sc1.g, sc1.b)), 0.55) * 0.95;
        vec3 sc0 = sc1 * 0.38;
        vec3 sc2 = mix(sc1, vec3(1.0), 0.50);
        c0 = mix(c0, sc0, spriteW);
        c1 = mix(c1, sc1, spriteW);
        c2 = mix(c2, sc2, spriteW);
    }
    float h = clamp(pow(clamp(e, 0.0, 1.0), 1.08) * M * 1.68 + I * 0.20 + streak * 0.14, 0.0, 1.0);
    vec3 col = mix(c0, c1, smoothstep(0.02, 0.48, h));
    col = mix(col, c2, smoothstep(0.34, 0.78, h));
    if (uCIPrismatic > 0.001 && M > 0.0) {
        vec3 rainbow = hsv2rgb(vec3(fract(0.04 + t * 0.72 + e * 0.07 + uCITime * 0.105), 0.84, 0.96));
        float rgbFringe = smoothstep(0.56, 0.76, e) * (1.0 - smoothstep(0.91, 1.0, e)) * M;
        col = mix(col, rainbow, clamp(uCIPrismatic, 0.0, 1.0) * rgbFringe * 0.6);
    }

    float pulse = 0.92 + 0.08 * sin(uCITime * 5.5);
    float styleGain = (abs(uCIStyle - 1.0) < 0.45) ? 1.18 : 1.0;
    if (abs(uCIStyle - 2.0) < 0.45) styleGain = 1.08;
    float a = clamp(I * 1.05 * uCIAlpha * uCIStrength * pulse * styleGain, 0.0, 0.82);
    vec3 outC = mix(src, col, a);
    outC += col * H * uCIHalo * 0.42 * uCIAlpha * uCIStrength * pulse;
    outC += c2 * (pow(h, 2.6) * I * 0.25 + outerRim * 0.16 + innerRim * 0.05 + streak * 0.12) * uCIAlpha * uCIStrength;
    outC += vec3(1.0) * outerRim * 0.10 * uCIAlpha * uCIStrength;

    fragColor = vec4(clamp(outC, 0.0, 1.0), 1.0);
}
""";

    public override string Source => ShaderGlsl;

    bool idleSent;

    public override void SetDefaults()
    {
        Fullscreen = true;
        Duration = 0;
        FadeIn = 2;
        Radius = 300;
    }

    public override void Draw(ScreenCommands cmd)
    {
        if (!(CoolerItemFx.Alpha > 0.01f))
            return;
        if (!CoolerItemFx.Stab && MathF.Abs(CoolerItemFx.Span) < 0.035f)
            return;
        base.Draw(cmd);
    }

    public override void Update()
    {
        var u = Uniforms;

        // Idle: nothing changes and Draw does not run, so nothing is sent.
        if (!(CoolerItemFx.Alpha > 0.01f))
        {
            if (!idleSent)
            {
                u["uCIAlpha"] = 0f;
                idleSent = true;
            }
            return;
        }
        idleSent = false;

        int sw = Main.screenWidth, sh = Main.screenHeight;
        if (sw <= 0 || sh <= 0)
            return;
        Vector2 sp = Main.screenPosition;

        // The shader's UV has y up; the world has y down.
        u["uCICenter"] = new Vector2((CoolerItemFx.Cx - sp.X) / sw, 1f - (CoolerItemFx.Cy - sp.Y) / sh);
        float head = CoolerItemFx.Head;
        u["uCISpan"] = CoolerItemFx.Span;
        float len = CoolerItemFx.Len / sh;
        u["uCILen"] = len;
        u["uCIInvLen"] = 1f / MathF.Max(len, 0.0001f);
        float ys = Math.Clamp(CoolerItemFx.VerticalScale, 0.5f, 2.4f);
        float xs = Math.Clamp(CoolerItemFx.XScale, 1f, 2f);
        u["uCIVerticalStretch"] = ys;
        u["uCIXStretch"] = xs;

        float sinH = MathF.Sin(head), cosH = MathF.Cos(head);
        u["uCIBladeDir"] = new Vector2(cosH, sinH);
        u["uCIHeadMetric"] = MathF.Atan2(sinH / ys, cosH / xs);
        u["uCISprite"] = CoolerItemFx.SpriteColor ? 1f : 0f;
        u["uCIChroma"] = Math.Clamp(CoolerItemFx.Chroma, 0f, 0.5f);
        u["uCIGrow"] = CoolerItemFx.Grow;

        u["uCITime"] = (float)(Main.GameUpdateCount / 60.0 % 1000.0);
        u["uCIAlpha"] = CoolerItemFx.Alpha;
        u["uCIStrength"] = CoolerItemFx.Strength;
        u["uCIDistort"] = CoolerItemFx.Distort;
        u["uCIHalo"] = CoolerItemFx.Halo;
        float[] r = CoolerItemFx.Ramp;
        u["uCIAR"] = r[0]; u["uCIAG"] = r[1]; u["uCIAB"] = r[2];
        u["uCIBR"] = r[3]; u["uCIBG"] = r[4]; u["uCIBB"] = r[5];
        u["uCICR"] = r[6]; u["uCICG"] = r[7]; u["uCICB"] = r[8];
        u["uCIPrismatic"] = CoolerItemFx.Prismatic ? 1f : 0f;
        u["uCIStyle"] = (float)CoolerItemFx.Style;
        u["uCIProgress"] = CoolerItemFx.Progress;
        u["uCIStab"] = CoolerItemFx.Stab ? 1f : 0f;
        u["uCICut"] = CoolerItemFx.Cut ? 1f : 0f;
        u["uCIRapidStab"] = CoolerItemFx.RapidStab ? 1f : 0f;
        u["uCISqueeze"] = CoolerItemFx.Squeeze;
    }
}
