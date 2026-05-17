# 2026-05-17 Canvas Preview Full Auto Scale

- Changed Canvas Preview `Auto` fit from a padded 90% viewport target to the
  full Preview viewport.
- Kept the fixed-scale behavior: `Auto` computes the current fit once, then the
  resulting scale persists across later Canvas previews until changed again.

Validation:

- Targeted App ViewModel and Avalonia Headless Canvas Preview tests passed.
- Headless screenshot artifact:
  `/private/tmp/wcx-full-auto-scale-headless/main-window-canvas-preview-auto-viewport-900x640.png`.
- Full solution build/test passed.
