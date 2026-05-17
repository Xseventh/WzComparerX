# 2026-05-17 Canvas Preview Fixed Scale

- Changed Canvas Preview scale from live `Auto` mode to a fixed current scale.
- Preview now defaults to `1x`.
- Clicking `Auto` fits the current image to the current Preview viewport once,
  then stores that computed scale for subsequent images.
- Added explicit `0.25x` and `0.5x` scale controls.

Validation:

- Targeted App ViewModel and Avalonia Headless Canvas Preview tests passed.
- Headless screenshot artifact:
  `/private/tmp/wcx-fixed-scale-headless/main-window-canvas-preview-auto-viewport-900x640.png`.
- Full solution build/test passed.
