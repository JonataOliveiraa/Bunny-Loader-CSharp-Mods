uniform float uDistance;
uniform float uGain;

void main() {
    vec2 offset = 2.5 / uResolution * uDistance;
    vec3 sum = texture(uSource, vUV + offset).rgb
        + texture(uSource, vUV - offset).rgb
        + texture(uSource, vUV + vec2(offset.x, -offset.y)).rgb
        + texture(uSource, vUV + vec2(-offset.x, offset.y)).rgb;
    fragColor = vec4(min(sum * 0.25 * uGain, 1.0), 1.0);
}
