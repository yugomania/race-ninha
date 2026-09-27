# Animal Combat Racing — URP Mobile Quality Profiles

Target Reference Benchmark: **Samsung Galaxy A53 5G** (Exynos 1280 / Mali-G68)

---

## 1. Quality Tier Specifications

| Setting | LOW Profile | MEDIUM Profile (Default) | HIGH Profile |
| :--- | :--- | :--- | :--- |
| **Target Frame Rate** | 60 FPS | 60 FPS | 60 FPS |
| **Render Scale** | 0.85x ($918\text{p}$) | 0.95x ($1026\text{p}$) | 1.0x ($1080\text{p}$) |
| **MSAA** | Disabled | 2x | 2x |
| **HDR** | Disabled | Disabled | Enabled (Mobile HDR) |
| **Directional Shadows** | Disabled (Blob shadow proxy) | Hard Shadows (1 Cascade, 30m) | Soft Shadows (1 Cascade, 45m) |
| **Shadow Map Resolution**| N/A | $1024 \times 1024$ | $2048 \times 2048$ |
| **Post-Processing** | Disabled | Bloom (Low precision) | Bloom + Color Grading |
| **Particle Budget** | Max 40 active particles | Max 80 active particles | Max 150 active particles |
| **Target Devices** | Low-end (Snapdragon 4xx/6xx) | Mid-tier (Galaxy A53, Redmi Note) | High-end (Snapdragon 8xx, iPhone) |

---

## 2. Universal Render Pipeline Asset Guidelines
* **Forward Renderer**: Single-Pass Forward rendering.
* **SRP Batcher**: Enabled across all materials. All custom shaders must declare standard `UnityPerMaterial` CBUFFER blocks.
* **Opaque Texture**: Disabled (saves full-screen blit bandwidth).
* **Depth Texture**: Enabled only if needed for water foam, otherwise disabled.
