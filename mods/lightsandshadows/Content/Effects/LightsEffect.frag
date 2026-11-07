uniform sampler2D uGradient;
uniform sampler2D uRays;
uniform sampler2D uBloomTexture;
uniform vec2 uSun;
uniform float uLightOn;
uniform float uClock;
uniform float uLightIntensity;
uniform float uBloomStrength;
uniform float uBloomPower;

vec3 Sunlight(vec2 uv) {
    vec2 axis = (uv - uSun) * vec2(uAspect, 1.0);
    float reach = (3.0 - length(axis)) / 3.0 * uLightIntensity;
    vec4 light = clamp(texture(uGradient, vec2(uClock, 0.5)) * reach, 0.0, 1.0);
    vec3 rays = texture(uRays, uv).rgb;
    return clamp(light.rgb * light.a - rays, 0.0, 1.0) * (light.a * light.a);
}

void main() {
    vec3 lit = blScreen(vUV).rgb;
    if (uLightOn > 0.5) lit = min(lit + Sunlight(vUV), 1.0);
    if (uBloomStrength > 0.0) {
        float luma = dot(lit, vec3(0.3, 0.6, 0.1));
        vec3 glow = texture(uBloomTexture, vUV).rgb * uBloomStrength * pow(clamp(1.0 - luma, 0.0, 1.0), uBloomPower);
        lit = min(lit + glow, 1.0);
    }
    fragColor = vec4(lit, 1.0);
}
