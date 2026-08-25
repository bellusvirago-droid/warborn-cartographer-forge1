# THE ASSET ROLL & THE BINDING

By the Founder's decree, Phase II of THE WARBORN MARCH is constructed purely from the approved purse and the Cartographer's own hand. No Editor shall be opened to map these assets; all configurations are codified for the headless build runner.

## I. The Binding Table

Every visual and spatial element of the Sundered Ford slice is bound to the Founder's purse below. 

| Element | Source Asset Path | Material / Modifier Notes |
| :--- | :--- | :--- |
| **Grogen Line** | `Assets/EuropeanKnightsPack01/Prefabs/Knight_A.prefab` | Swap `Mat_Knight` to Cartographer-authored `Mat_Grogen_EarthIron`. |
| **Daminari Legion** | `Assets/EuropeanKnightsPack01/Prefabs/Knight_B.prefab` | Swap `Mat_Knight` to Cartographer-authored `Mat_Daminari_CrimsonSteel`. |
| **Sundered Ford (East High Ground)** | `Assets/MedievalCastleKit/Prefabs/Walls/Wall_Broken_A.prefab` | Transform scaled/rotated via script to form the cliff embankment. |
| **Sundered Ford (West Dig)** | `Assets/MedievalCastleKit/Prefabs/Props/Rubble_Pile.prefab` | Arrayed via headless scatter script to designate diggable terrain. |
| **War Table Hall** | `Assets/MedievalCastleKit/Prefabs/Structures/Keep_Interior.prefab` | Serves as the Muster stage before Strike reckoning. |
| **Combat Locomotion** | `Assets/KevinIglesias/HumanBasicMotionsFREE/Animations/` | Applied to the Unity Humanoid rig via `AnimatorOverrideController`. |
| **Strike & Hit Reactions** | `Assets/KevinIglesias/HumanMeleeAnimations/Animations/` | Bound directly to the `StrikeReckoner` state events. |
| **Ice Magic (Live)** | `Assets/UltimateVFX/Prefabs/Ice/Ice_Spikes_01.prefab` | Spawned via `CastIce()`. Particle emission modified. |
| **Torch Fire** | `Assets/UltimateVFX/Prefabs/Fire/Torch_Flame.prefab` | **CRITICAL:** Flicker rate clamped to max 3 Hz via processor script. |
| **Blood Impacts** | `Assets/UltimateVFX/Prefabs/Blood/Blood_Hit_Directional.prefab` | Aligned to swing normals calculated by the `StrikeReckoner`. |
| **Dust on the Ford** | `Assets/UltimateVFX/Prefabs/Dust/Dust_Ambient.prefab` | Ground level, low opacity, continuous slow particle drift. |

## II. The Gaps (Authored In-House by the Cartographer)

No owned pack fulfills the entirety of the March's laws. The following are authored in-house as headless scripts or programmatic generated assets:

1.  **The SKU Blades (The Return Current Binding):** The `EuropeanKnightsPack01` generic swords are stripped at runtime. A headless script `SKUWeaponImporter.cs` downloads and caches the precise, patented GLTF weapon models from the armoury based on the winning SKU, binding them dynamically to the `RightHand` Humanoid bone. A player tests *only* the exact piece sold.
2.  **The Dragon Contract Manifestation:** As no dragon model exists in the purse, the dragon is implemented as a shadow pass (`Projector` component via script) sweeping over the Sundered Ford, accompanied by a Cartographer-authored synthesized roar. The `DragonContract` class remains strictly sealed with no path to betrayal.
3.  **The Faction Liveries:** `Mat_Grogen_EarthIron` and `Mat_Daminari_CrimsonSteel` are authored via a script that generates URP Material assets, assigning flat tint vectors to the standard albedo maps of the Knight pack.
4.  **Audio Projections:** Synthesized strike, parry, ice shatter, and the dragon roar are generated as `.wav` buffers at build time by a Cartographer-written procedural audio script, as no sound pack resides in the purse.

## III. The Headless Runner Binding Map (`build_bindings.json`)

This JSON map is parsed by `AssetPostprocessor` and prefab-generation scripts in the CI/CD pipeline to construct the game objects without human intervention.

```json
{
  "schema_version": "1.0",
  "target_slice": "sundered_ford",
  "materials": [
    {
      "id": "mat_grogen",
      "shader": "Universal Render Pipeline/Lit",
      "base_color": "#4A3B2C",
      "metallic": 0.8
    },
    {
      "id": "mat_daminari",
      "shader": "Universal Render Pipeline/Lit",
      "base_color": "#8B0000",
      "metallic": 0.9
    }
  ],
  "character_bindings": [
    {
      "faction": "Grogen",
      "base_prefab": "Assets/EuropeanKnightsPack01/Prefabs/Knight_A.prefab",
      "material_override": "mat_grogen",
      "rig": "Humanoid",
      "animator_controller": "Assets/Cartographer/Animators/CombatController.controller",
      "weapon_bone": "Character_RightHand",
      "weapon_binding": "RUNTIME_SKU_FETCH"
    },
    {
      "faction": "Daminari",
      "base_prefab": "Assets/EuropeanKnightsPack01/Prefabs/Knight_B.prefab",
      "material_override": "mat_daminari",
      "rig": "Humanoid",
      "animator_controller": "Assets/Cartographer/Animators/CombatController.controller",
      "weapon_bone": "Character_RightHand",
      "weapon_binding": "RUNTIME_SKU_FETCH"
    }
  ],
  "vfx_overrides": [
    {
      "asset": "Assets/UltimateVFX/Prefabs/Fire/Torch_Flame.prefab",
      "property_modifications": {
        "flicker_frequency_max_hz": 3.0,
        "color_strobe_disabled": true
      },
      "rule_compliance": "Stillness setting and photosensitivity limit (<= 3Hz)."
    }
  ],
  "animation_clips": {
    "idle": "Assets/KevinIglesias/HumanBasicMotionsFREE/Animations/Idle.anim",
    "walk": "Assets/KevinIglesias/HumanBasicMotionsFREE/Animations/Walk.anim",
    "strike": "Assets/KevinIglesias/HumanMeleeAnimations/Animations/1H_Melee_Attack_Chop.anim",
    "death": "Assets/KevinIglesias/HumanMeleeAnimations/Animations/1H_Melee_Death_Forward.anim"
  }
}
```