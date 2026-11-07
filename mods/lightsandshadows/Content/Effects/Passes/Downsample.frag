uniform float uThreshold;

void main() {
    vec3 color = texture(uSource, vUV).rgb;
    float mask = color.b < uThreshold ? pow(1.0 - color.b, 2.25) : 0.0;
    fragColor = vec4(color, mask);
}
