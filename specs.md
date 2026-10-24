# Technical Specification: The Ink and the Ash

## 1\. System Architecture & Core Stack

* **Game Engine:** Unity 6  
* **Render Pipeline:** Universal Render Pipeline (URP)  
* **Target Platforms:** Android (Mobile) and Windows PC (Desktop)  
* **Target Performance:** Stable 60 FPS with zero-allocation garbage collection tuning and aggressive occlusion culling.  
* **Asset Integrations:** Synty Fantasy Village and Synty Biomes asset packs; Pixel Crushers Love/Hate social simulation engine.

## 2\. Rendering & Platform Optimizations

* **GPU Instancing:** Enabled across all environmental foliage and repetitive structural props from Synty asset packs to minimize draw calls.  
* **Texture Compression Formats:**  
  * **Android:** ASTC (Adaptive Scalable Texture Compression) for balanced memory footprint and visual fidelity.  
  * **Windows PC:** BC7 compression for high-quality texture output.  
* **Lighting Strategy:** Baked lightmaps combined with lightweight real-time directional shadows for mobile thermal management and consistent desktop performance.

## 3\. Input & Accessibility Architecture

* **Cross-Platform Input Binding:** Unified input abstraction layer utilizing Unity's Input System package.  
* **PC Controls:** Point-and-click mouse navigation (left-click to move, inspect, and interact) with expanded raycast hitboxes and interaction magnetism. WASD and standard gamepads supported as full operational backups.  
* **Mobile Controls:** Touch-driven tap-to-move navigation and direct tap interaction prompts.  
* **Motor Accessibility:** Fully remappable inputs across all devices supporting adaptive controllers. All persistent actions utilize press-to-toggle states rather than continuous holding. Optional "Auto-Follow Road" mode for automated overworld paper map travel.

## 4\. Audio Architecture & Generation

* **Adaptive Music (Google Lyria):** Procedural/dynamic soundtrack generation featuring distinct biome exploration themes, town/city tracks, Spire ascent music, and high-energy combat arrangements that dynamically transition from cozy wandering melodies.  
* **Sound Effects (SFX):** Integrated commercial Unity SFX asset pack for UI interactions, weather ambiance, and combat impacts.

## 5\. Gameplay & Data Systems

* **Caravan Inventory:** Clean, non-spatial list-based ledger tracking 16 core tradable items. Logistical pressure governed by overworld weather, real-time temperature multipliers, and perishable decay rates (1% base/min, modulated by biome conditions).  
* **Campsite Cooking:** Streamlined ingredient selection interface converting raw perishables into durable meals granting 24-hour regional expedition buffs.  
* **Combat System (CTB):** Conditional Turn-Based combat modeled after *Final Fantasy X* with a visible timeline UI. Turn order calculated via `Base Turn Delay = (Skill Cost / Agility) * Weather Multiplier`.  
* **Data Serialization:** All dialogue trees, quest arcs (5-Act Architecture), faction parameters, and PAD emotional states serialized into human-readable JSON files for runtime loading in Unity and Git version control synchronization.