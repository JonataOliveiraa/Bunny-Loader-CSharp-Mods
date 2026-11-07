uniform sampler2D uGradient;
uniform sampler2D uRays;
uniform vec2 uSun;
uniform float uLightOn;
uniform float uClock;
uniform float uLightIntensity;
uniform float uThreshold;
uniform float uGain;

vec3 Sunlight(vec2 uv) {
    vec2 axis = (uv - uSun) * vec2(uAspect, 1.0);
    float reach = (3.0 - length(axis)) / 3.0 * uLightIntensity;
    vec4 light = clamp(texture(uGradient, vec2(uClock, 0.5)) * reach, 0.0, 1.0);
    vec3 rays = texture(uRays, uv).rgb;
    return clamp(light.rgb * light.a - rays, 0.0, 1.0) * (light.a * light.a);
}

void main() {
    vec3 lit = texture(uSource, vUV).rgb;
    if (uLightOn > 0.5) lit = min(lit + Sunlight(vUV), 1.0);
    float peak = max(lit.r, max(lit.g, lit.b));
    vec3 kept = peak > uThreshold ? lit : vec3(0.0);
    fragColor = vec4(min(kept * uGain, 1.0), 1.0);
}
