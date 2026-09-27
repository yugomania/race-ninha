# Animal Combat Racing — Mobile Optimization Guide

## 1. Budget Benchmarks
* **Draw Calls**: $< 100$ per frame
* **Triangle Count**: $< 80,000$ active vertices on-screen
* **Frame Rate**: Stable 60 FPS on mid-tier Android (Snapdragon 680+) and iOS (A13 Bionic+)
* **Texture Memory**: $< 150 \text{ MB}$ compressed via ASTC

## 2. Universal Render Pipeline (URP) Settings
* **Rendering Path**: Forward Single-Pass
* **Shadows**: Directional Sun, 1 Cascade, max distance 40m
* **Post-Processing**: Minimal bloom and color grading; depth-of-field disabled on mobile
* **Garbage Collection Policy**: Zero runtime heap allocations in `Update()`, `FixedUpdate()`, and collision events.
