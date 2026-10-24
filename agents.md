# Agents & Development Guidelines: The Ink and the Ash

## 1\. Project Overview & Core Philosophy

* **Project Title:** The Ink and the Ash  
* **Genre:** Cozy-melancholic JRPG & Open-World Pilgrimage  
* **Engine & Pipeline:** Unity 6 with Universal Render Pipeline (URP).  
* **Target Platforms:** Android mobile devices and Windows PC, optimized for a buttery-smooth 60 FPS.  
* **Core Design Tenet:** Restorative stewardship over military conquest. Players unburden communities, manage perishables across 7 distinct biomes, calibrate regional Spires, and resolve conflicts via a Conditional Turn-Based (CTB) combat system.

---

## 2\. Technical & Performance Architecture (Unity 6 URP)

* **Rendering & Performance Targets:** Target 60 FPS stable on Android and PC. Utilize GPU instancing for Synty Fantasy Village and Biomes asset packs, static occlusion culling, baked lightmaps with lightweight real-time directional shadows, and zero-allocation garbage collection tuning.  
* **Texture Compression:** Use ASTC texture compression for Android builds and BC7 for Windows PC builds.  
* **Input Architecture:** Platform-agnostic design supporting point-and-click mouse navigation (left-click to move, inspect, and interact) and mobile tap-to-move. Fully remappable inputs across keyboard, mouse, and gamepads for adaptive controller support. WASD and gamepad movement are maintained as parallel backups.  
* **Accessibility Standards:** Expanded raycast hitboxes and interaction magnetism to eliminate strict pixel-precision requirements. Persistent actions utilize press-to-toggle states rather than continuous holding. Optional "Auto-Follow Road" mode for automated overworld paper map travel.

---

## 3\. Audio & Asset Generation Standards

* **Adaptive Soundtrack:** Custom biome music generated via Google Lyria, transitioning dynamically from relaxing, cozy exploration loops into active, rhythmic combat arrangements, alongside dedicated town, city, and Spire ascent tracks.  
* **Sound FX (SFX):** Integrated via a purchased professional Unity asset pack covering UI clicks, weather ambiance, and combat impacts.

---

## 4\. Gameplay, Inventory, & Economy Rules

* **Caravan Inventory:** Clean, straightforward list-based caravan ledger. The logistical challenge stems from overworld weather pressures, perishable decay, and resource management rather than spatial grid fitting.  
* **Campsite Cooking:** Streamlined recipe selection from available unified ingredients (16-item trade economy), instantly converting raw materials into day-long expedition buffs.  
* **Combat & Recovery:** CTB combat modeled after *Final Fantasy X* with a visible action timeline. Defeat results in a reload at the last campsite and temporary injury debuffs cleared through rest and cooked meals.

---

## 5\. Narrative & AI Style Guide Mandates

* **Tone:** Cozy-melancholic, grounded, empathetic, and reflective.  
* **Strict Prohibitions:** Never use emojis, em-dashes, Oxford commas, unnecessary adjectives, modern slang, or grandiose epic fantasy tropes.  
* **Focus:** Ground all narrative content in daily practical struggles (repairing equipment, sharing food, easing neighborly fear, and managing trade logistics).  
* **Data Structure:** All dialogue trees, quest states (5-Act Architecture), faction parameters, and PAD emotional states (Pixel Crushers Love/Hate Engine) are serialized into structured JSON files for seamless runtime loading in Unity.