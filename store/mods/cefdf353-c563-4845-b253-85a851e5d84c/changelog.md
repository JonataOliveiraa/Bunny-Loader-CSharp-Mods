## 2.0.0

Ported from JavaScript to C# on 2026-11-06, with the same shaders, passes, numbers and defaults. C# configs have no conditional greying or slider suffixes, so every option is always editable and plain.

# Changelog

## 1.0.0

- Port of Lights And Shadows 1.4.2: sunlight and moonlight gradients, shadow rays, bloom and the original options.
- Rebuilt for phones: sunlight computed in the final pass, shadow rays in one pass, shared downsampled screen, threshold merged with the first blur, light and bloom added together.
- New option: shadow quality (High, Medium, Low).
