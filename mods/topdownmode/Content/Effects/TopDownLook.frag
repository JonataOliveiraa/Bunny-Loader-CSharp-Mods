#include <bl/screen>

uniform float uDay;
uniform float uGrade;

void main() {
    vec3 col = blScreen(vUV).rgb;

    if (uGrade > 0.0) {
        vec3 tint = mix(vec3(0.93, 0.97, 1.07), vec3(1.04, 1.01, 0.95), uDay);
        col *= mix(vec3(1.0), tint, uGrade);
        float lum = dot(col, vec3(0.299, 0.587, 0.114));
        col = mix(vec3(lum), col, 1.0 + 0.10 * uGrade);
        vec2 d = (vUV - 0.5) * vec2(1.0, 0.9);
        col *= 1.0 - smoothstep(0.40, 0.88, length(d)) * 0.16 * uGrade;
    }

    fragColor = vec4(clamp(col, 0.0, 1.0), 1.0);
}
