# 2026-05-17 Canvas Preview Viewport Auto Scale

- Changed Avalonia Canvas Preview `Auto` scale to use the current Preview
  viewport size instead of only the bitmap dimensions.
- Auto scale now enlarges small bitmaps with capped integer scale when they fit
  the viewport and shrinks large bitmaps fractionally to avoid immediate
  scroll-only previews.
- Manual `1x`, `2x`, `4x`, `8x`, and `16x` controls remain explicit overrides.

Validation:

- Targeted App ViewModel and Avalonia Headless Canvas Preview tests passed.
- Headless screenshot artifact:
  `/private/tmp/wcx-auto-scale-headless/main-window-canvas-preview-auto-viewport-900x640.png`.
- Full solution build/test passed.
