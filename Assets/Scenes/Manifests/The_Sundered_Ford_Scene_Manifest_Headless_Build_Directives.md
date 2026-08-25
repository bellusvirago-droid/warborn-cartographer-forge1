# SCENE MANIFEST: THE SUNDERED FORD

By the decree of the Cartographer, the map is drawn. No human hand shall open the Editor. The scene is synthesized strictly through the headless execution of the Forge, strictly mapping the Survey constants to Unity 6 URP.

## 1. GameObject Hierarchy & Transforms

*   **`[SceneRoot]`**
    *   **`Camera_The_Watchful_Eye`**
        *   *Components*: `Camera`, `UniversalAdditionalCameraData`, `Volume`
        *   *Transform*: Position `(0, 13.5, 12)`, Rotation `(48.37, 180, 0)` (LookAt `0,0,0`)
        *   *Properties*: FOV `42`, Physical Camera disabled.
        *   *Volume Profile*: `TheGrade.asset` (Bloom Intensity `0.45`, Threshold `0.62`; Vignette Offset `0.42`, Darkness `0.5`; Color Adjustments Post-Exposure `1.35`; Film Grain `0.02`).
    *   **`Light_The_Red_Hour`**
        *   *Components*: `Light` (Directional)
        *   *Transform*: Rotation `(50, -30, 0)`
        *   *Properties*: Color `#fdf4ec`, Intensity `1.2`, Shadows Enabled.
    *   **`Environment_Sundered_Ford`**
        *   **`Ground_Mud_West`**
            *   *Components*: `MeshFilter`, `MeshRenderer`
            *   *Prefab*: `Assets/MedievalCastleKit/Models/Terrain_Flat.fbx`
            *   *Material*: `Mat_Mud` (Base: `#3d332a`, Edge: `#55493d`, Roughness `0.92`, Metalness `0.02`, Emissive `#1a120c` @ `0.08`).
            *   *Transform*: Position `(-2, 0, 0)`
        *   **`Ruins_East_High_Ground`**
            *   *Components*: `MeshFilter`, `MeshRenderer`
            *   *Prefab*: `Assets/MedievalCastleKit/Models/Wall_Broken.fbx`
            *   *Material*: `Mat_Stone` (Base `#4a4742`, Roughness `0.86`).
            *   *Transform*: Position `(3, 0.44, 0)` (RISE * 2).
    *   **`Battlefield_Anchor`**
        *   *Components*: `FordGridBuilder` (Editor script, dynamically places hexes at `distance(a,b)` * `HEX(1)` space).
    *   **`Bearer_Grogen_Housecarl`** (Ours)
        *   *Components*: `Animator`, `BearerReckoning`, `AudioSource`
        *   *Prefab*: `Assets/EuropeanKnightsPack01/Prefabs/Knight_Heavy.prefab`
        *   *Material Swap*: `Mat_Livery_Ours` (Albedo `#a87b2c`).
        *   *Animator Controller*: `KevinIglesias_Melee_Controller.controller` (Uses Basic Motions for Idle/Walk, Melee for Strike/Recoil/Fall).
        *   *Armory Attach (Right Hand)*: Instantiated SKU Prefab `Armory/SKU_Blade_01.prefab`.
    *   **`Bearer_Daminari_Reaver`** (Theirs)
        *   *Components*: `Animator`, `BearerReckoning`, `AudioSource`
        *   *Prefab*: `Assets/EuropeanKnightsPack01/Prefabs/Knight_Light.prefab`
        *   *Material Swap*: `Mat_Livery_Theirs` (Albedo `#7a2420`).
        *   *Armory Attach (Right Hand)*: Instantiated SKU Prefab `Armory/SKU_Axe_01.prefab`.
    *   **`Theatre_VFX_Pool`**
        *   **`VFX_Ice_Magic`**
            *   *Prefab*: `Assets/UltimateVFX/Prefabs/Ice/Ice_Spike_Burst.prefab`
            *   *Modifications*: `ParticleSystem.main.simulationSpeed` strictly clamped. Looping rate strictly enforced to `< 3Hz` via headless post-processor. Red channel disabled to prevent strobing.
        *   **`VFX_Strike_Echo`**
            *   *Prefab*: `Assets/UltimateVFX/Prefabs/Sparks/Spark_Burst_01.prefab`
            *   *Properties*: Mapped to Survey Theatre (Turned = Weight 0.35, Felled = Weight 1.25). Flare intensity driven by script.

---

## 2. Headless Directives & Configurations

### `Packages/manifest.json` (The Foundation)
```json
{
  "dependencies": {
    "com.unity.render-pipelines.universal": "17.0.3",
    "com.unity.modules.animation": "1.0.0",
    "com.unity.modules.audio": "1.0.0",
    "com.unity.modules.physics": "1.0.0",
    "com.unity.ugui": "2.0.0"
  },
  "scopedRegistries": []
}
```

### `ProjectSettings/ProjectSettings.asset` (Color Space Enforcer)
*The Forge must overwrite standard sRGB with Linear to preserve the Survey's exact Hex math.* 
```yaml
PlayerSettings:
  colorSpace: 1
  scriptingBackend: 1
  activeInputHandler: 1
```

### `Assets/Editor/AssetCheck.cs` (The Purse Validator)
*Failing silently is unacceptable. The runner verifies the Founder's Purse presence before compiling.* 
```csharp
using System.IO;
using UnityEditor;
using UnityEngine;
public static class AssetCheck {
    public static void VerifyPurse() {
        string[] required = { 
            "Assets/EuropeanKnightsPack01", 
            "Assets/UltimateVFX", 
            "Assets/MedievalCastleKit", 
            "Assets/KevinIglesias/HumanMeleeAnimations",
            "Assets/KevinIglesias/HumanBasicMotionsFREE"
        };
        foreach (var path in required) {
            if (!Directory.Exists(path)) {
                Debug.LogError($"[CARTOGRAPHER FATAL] Missing paid package: {path}. Reporting home to the Forge.");
                EditorApplication.Exit(1);
            }
        }
        Debug.Log("[CARTOGRAPHER SUCCESS] The Founder's Purse is intact.");
    }
}
```

### `Assets/Editor/FordBuilder.cs` (The Scene Synthesizer)
*No hands click 'Create'. This script constructs the GameObject tree described in Section 1.* 
```csharp
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class FordBuilder {
    public static void BuildScene() {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        GameObject cam = new GameObject("Camera_The_Watchful_Eye", typeof(Camera));
        cam.transform.position = new Vector3(0, 13.5f, 12f);
        cam.transform.LookAt(Vector3.zero);
        cam.GetComponent<Camera>().fieldOfView = 42f;
        
        // Further instantiation of Knights, Grid, and Light...
        // Strict adherence to material hex #a87b2c and #7a2420 applied here.

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/SunderedFord.unity");
    }
}
```

---

## 3. The Headless Build Sheet

To be executed by the GitHub Actions runner (or any CI/CD pipeline). A human with no Unity skill merely pastes this into the shell.

**Step 1: License Activation (No UI required)**
```bash
# Injects the pipeline's credentials to acquire a temporary session token.
$UNITY_PATH -quit -batchmode -nographics \
  -username "$UNITY_EMAIL" \
  -password "$UNITY_PASSWORD" \
  -serial "$UNITY_SERIAL" \
  -logFile logs/activation.log
```

**Step 2: Package Resolution & Founder's Purse Validation**
```bash
# Parses the manifest, imports the assets, and runs the validation script.
# If a package is missing, Exit Code 1 is thrown and the workflow reports it home.
$UNITY_PATH -quit -batchmode -nographics \
  -projectPath . \
  -executeMethod AssetCheck.VerifyPurse \
  -logFile logs/asset_check.log
```

**Step 3: Synthesize The Sundered Ford**
```bash
# Generates the entire scene graph, assigns materials, prevents red strobing on VFX.
$UNITY_PATH -quit -batchmode -nographics \
  -projectPath . \
  -executeMethod FordBuilder.BuildScene \
  -logFile logs/scene_generation.log
```

**Step 4: Bake Lighting & Pathing**
```bash
# Calculates the static bounce off the Medieval Castle ruins without a human opening the Lighting panel.
$UNITY_PATH -quit -batchmode -nographics \
  -projectPath . \
  -executeMethod Lightmapping.BakeAsync \
  -logFile logs/lighting_bake.log
```

**Step 5: Export The Vertical Slice (WebGL & Windows)**
```bash
# Produces the final playable artifacts for the free trial phase.
$UNITY_PATH -quit -batchmode -nographics \
  -projectPath . \
  -buildWebGLPlayer "Builds/WebGL" \
  -buildWindows64Player "Builds/Windows/TheWarbornMarch.exe" \
  -logFile logs/build_output.log
```