uniform vec2 uSun;
uniform float uTapCount;
uniform vec4 uTaps[16];

float Occluder(vec2 uv) {
    if (uv.x < 0.0 || uv.y < 0.0 || uv.x > 1.0 || uv.y > 1.0) return 0.0;
    return texture(uSource, uv).a;
}

void main() {
    vec2 offset = vUV - uSun;
    int count = int(uTapCount);
    float sum = 0.0;
    for (int i = 0; i < 16; i++) {
        int first = i * 2;
        if (first >= count) break;
        vec4 taps = uTaps[i];
        sum += Occluder(uSun + offset * taps.x) * taps.y;
        if (first + 1 < count) sum += Occluder(uSun + offset * taps.z) * taps.w;
    }
    fragColor = vec4(vec3(min(sum, 1.0)), 1.0);
}
