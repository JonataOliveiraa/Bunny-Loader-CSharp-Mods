#include <bl/screen>

#define ROWS 80
#define HALF 40
uniform vec4 uRowsA[HALF];
uniform vec4 uRowsB[HALF];
uniform vec2 uRel0;
uniform vec2 uAbs0;
uniform vec2 uWorldSize;
uniform float uStrength;
uniform float uSpeed;
uniform float uDay;
uniform float uDeepCaustic;
uniform vec4 uSwim[4];

bool isWaterCol(vec3 c) {
    return c.b > c.r * 1.2 + 0.015 && c.b >= c.g * 0.97;
}

void run(float p, float x, inout float inside, inout float edge) {
    float s = floor(p / 512.0);
    float e = p - s * 512.0;
    float x0 = s * 16.0;
    float x1 = e * 16.0;
    float hit = step(s + 0.5, e) * step(x0, x) * (1.0 - step(x1, x));
    inside = max(inside, hit);
    edge = mix(edge, min(x - x0, x1 - x), hit);
}

vec2 scanRow(int row, float x) {
    if (row < 0 || row >= ROWS) return vec2(0.0, 0.0);
    vec4 v = row >= HALF ? uRowsB[row - HALF] : uRowsA[row];
    float inside = 0.0;
    float edge = 9999.0;
    run(v.x, x, inside, edge);
    run(v.y, x, inside, edge);
    run(v.z, x, inside, edge);
    run(v.w, x, inside, edge);
    return vec2(inside, edge);
}

void main() {
    vec2 px = vec2(vUV.x * uWorldSize.x, (1.0 - vUV.y) * uWorldSize.y);
    vec2 rel = uRel0 + px;
    vec2 wp = uAbs0 + px;
    float t = uTime * uSpeed;
    float rowF = floor(rel.y / 16.0);
    int row = int(rowF);

    vec2 h = scanRow(row, rel.x);
    if (h.x < 0.5) discard;
    vec3 c0 = blScreen(vUV).rgb;
    if (!isWaterCol(c0)) discard;

    float fy = rel.y - rowF * 16.0;
    float dH = h.y;
    float dV = 16.0;
    if (fy < 6.0) {
        if (scanRow(row - 1, rel.x).x < 0.5) dV = fy;
    } else if (fy > 10.0) {
        if (scanRow(row + 1, rel.x).x < 0.5) dV = 16.0 - fy;
    }
    float d = min(dH, dV);

    vec2 q = floor((uAbs0 + px) * 0.5) * 2.0;

    float inBody = 0.0;
    float ripple = 0.0;
    for (int i = 0; i < 4; i++) {
        vec4 s = uSwim[i];
        if (s.w > 0.5) {
            float dx = wp.x - s.x;
            inBody = max(inBody, step(abs(dx), s.z + 2.0) * step(wp.y, s.y + 2.0) * step(s.y - 60.0, wp.y));
            vec2 e = vec2(dx / (s.z * 1.8), (wp.y - s.y) / (s.z * 0.8));
            float r = length(e);
            for (int k = 0; k < 2; k++) {
                float ph = fract(t * 0.4 + float(k) * 0.5);
                float rr = 0.55 + ph * 0.95;
                ripple = max(ripple, (1.0 - ph) * (1.0 - smoothstep(0.0, 0.10, abs(r - rr))) * step(-0.12, e.y));
            }
        }
    }

    float amp = min(uStrength * uIntensity, max(d - 1.0, 0.0) * 0.8) * (1.0 - inBody);
    vec2 off = vec2(sin(q.y * 0.115 + t * 1.5), sin(q.x * 0.093 - t * 1.2 + 1.7));
    off = floor(off * amp + 0.5);
    vec2 uvS = vUV + vec2(off.x / uWorldSize.x, -off.y / uWorldSize.y);
    vec3 col = blScreen(uvS).rgb;
    if (!isWaterCol(col)) col = c0;
    float lum = dot(col, vec3(0.299, 0.587, 0.114));
    float ls = clamp(max(mix(0.16, 1.0, uDay), lum * 2.4), 0.0, 1.0);

    float a = sin(q.x * 0.071 + sin(q.y * 0.052 + t * 0.9) * 1.7 + t * 0.6);
    float b = sin(q.y * 0.083 + sin(q.x * 0.044 - t * 0.8) * 1.5 - t * 0.5);
    float caustic = 1.0 - smoothstep(0.0, 0.20, abs(a + b));
    float shallow = 1.0 - 0.45 * smoothstep(12.0, 96.0, dH);
    vec2 ds = vec2(7.0 / uWorldSize.x, 7.0 / uWorldSize.y);
    vec3 m = c0 + blScreen(vUV + vec2(ds.x, 0.0)).rgb + blScreen(vUV - vec2(ds.x, 0.0)).rgb
           + blScreen(vUV + vec2(0.0, ds.y)).rgb + blScreen(vUV - vec2(0.0, ds.y)).rgb;
    float gb = m.g / max(m.b, 0.001);
    float shallowCol = smoothstep(0.60, 0.69, gb);
    float causticMask = mix(clamp(uDeepCaustic, 0.0, 1.0), 1.0, shallowCol);
    col += vec3(0.62, 0.86, 1.0) * caustic * shallow * causticMask * 0.13 * uIntensity * ls;

    float breathe = 0.5 + 0.5 * sin(t * 1.6 + (uAbs0.x + px.x) * 0.021 + (uAbs0.y + px.y) * 0.027);
    float foamW = 2.0 + 2.5 * breathe;
    float foam = 1.0 - smoothstep(foamW - 1.0, foamW + 1.0, d);
    col = mix(col, vec3(0.90, 0.97, 1.0) * ls, foam * (0.30 + 0.30 * breathe) * uIntensity);
    col = mix(col, vec3(0.80, 0.95, 1.0) * ls, ripple * 0.45 * (1.0 - inBody));


    fragColor = vec4(col, 1.0);
}
