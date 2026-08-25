# THE WARBORN MARCH
## Phase II Vertical Slice Scene Manifest: The Sundered Ford

### I. SCENE HIERARCHY & GAMEOBJECT MANIFEST

*   **Main_Camera_Rig** (Transform: Pos[0, 10, -10], Rot[45, 0, 0])
    *   *Components:* `Transform`, `WarTableDirector` (Manages camera states: WarTableHome, StrikeFall, TurnRise)
    *   **Main_Camera** (Transform: LocalPos[0,0,0], LocalRot[0,0,0])
        *   *Components:* `Camera` (FOV: 60), `AudioListener`
*   **Lighting_Rig** (Transform: Pos[0, 20, 0], Rot[0, 0, 0])
    *   **Sun_Raking** (Transform: Pos[0, 0, 0], Rot[15, -45, 0])
        *   *Components:* `Light` (Type: Directional, Color: #FF9933 [Torch-warm], Intensity: 0.8, ShadowType: Soft)
    *   **Fill_Ambient** (Transform: Pos[0, 0, 0], Rot[75, 135, 0])
        *   *Components:* `Light` (Type: Directional, Color: #445566 [Dim steel-blue], Intensity: 0.2, ShadowType: None)
*   **Environment_SunderedFord** (Transform: Pos[0, 0, 0])
    *   **Terrain_West_Grogen** (Transform: Pos[-10, 0, 0])
        *   *Components:* `MeshFilter` (Prefab: `Env_DiggableEarth`), `MeshRenderer` (Material: `Mat_DarkMud`), `MeshCollider`
    *   **Terrain_East_Daminari** (Transform: Pos[10, 2, 0])
        *   *Components:* `MeshFilter` (Prefab: `Env_HighGroundRock`), `MeshRenderer` (Material: `Mat_Basalt`), `MeshCollider`
    *   **River_Broken** (Transform: Pos[0, -0.5, 0])
        *   *Components:* `MeshFilter` (Prefab: `Env_RiverBed`), `MeshRenderer` (Material: `Mat_FrozenWater`)
        *   *Note:* Houses the Ice Magic environmental visual.
*   **Armies_PhaseII** (Transform: Pos[0, 0, 0])
    *   **Faction_Grogens** (Transform: Pos[-5, 0, -2])
        *   *Components:* `MusterController` (Faction: Deep Dig)
        *   **Grogen_Champion** (Transform: Pos[0, 0, 0])
            *   *Components:* `Animator` (Stillness bound, max 3Hz)
            *   **Weapon_Socket_RealSKU** (Transform: LocalPos[0.5, 1, 0])
                *   *Components:* `MeshFilter` (Prefab: `SKU_Blade_Grogen_01`), `MeshRenderer` (Material: `Mat_ArmourySteel` - Metallic: 1.0, Smoothness: 0.9, bright specular)
    *   **Faction_Daminari** (Transform: Pos[5, 2, 2])
        *   *Components:* `MusterController` (Faction: Legion)
        *   **Daminari_Champion** (Transform: Pos[0, 0, 0])
            *   *Components:* `Animator` (Stillness bound, max 3Hz)
            *   **Weapon_Socket_RealSKU** (Transform: LocalPos[-0.5, 1, 0])
                *   *Components:* `MeshFilter` (Prefab: `SKU_Blade_Daminari_01`), `MeshRenderer` (Material: `Mat_ArmourySteel` - Metallic: 1.0, Smoothness: 0.9, bright specular)
*   **Mercenary_Dragon** (Transform: Pos[0, 15, 5])
    *   *Components:* `MeshFilter` (Prefab: `Unit_IceDragon`), `DragonContract` (SEALED CLASS - strict timing, no betrayal triggers)
*   **System_Core** (Transform: Pos[0, 0, 0])
    *   **Controller_StrikeReckoner**
        *   *Components:* `StrikeReckoner` (SEALED CLASS - calculates Vigour, Might, Guard, Magical)
    *   **Controller_ReturnCurrent**
        *   *Components:* `ReturnCurrentOffer` (Binds UI to winning SKU SKU_Blade_Grogen_01 or SKU_Blade_Daminari_01)
    *   **Controller_Stillness**
        *   *Components:* `StillnessGovernor` (Scans scene: truncates any animation looping < 0.33s, forces red lights to static)
*   **Canvas_UI** (Transform: Pos[0, 0, 0])
    *   *Components:* `Canvas` (RenderMode: ScreenSpaceOverlay), `CanvasScaler`
    *   **Panel_FreeTrial_Header**
        *   *Components:* `Text` (String: "THE WARBORN MARCH - FREE TRIAL SLICE")
    *   **Panel_Strike_Meters**
        *   *Components:* `UI_VigourMeter`, `UI_MightMeter`, `UI_GuardMeter`, `UI_MagicalMeter`
    *   **Panel_ReturnCurrent**
        *   *Components:* `UI_SKU_Offer` (Displays rising return current offer on winning blade)

---

### II. BUILD ORDER FOR ASSEMBLY

1.  **Initialize the Canvas:** Create a new Unity Scene named `SunderedFord_PhaseII`. Delete the default camera and light. Create an empty GameObject named `System_Core`. Attach the sealed scripts `StrikeReckoner.dll`, `DragonContract.dll`, and `StillnessGovernor.cs` to it.
2.  **Mount the War Table Camera:** Create an empty GameObject named `Main_Camera_Rig`. Set its Position to X:0, Y:10, Z:-10 and Rotation to X:45, Y:0, Z:0. Create a Unity Camera as a child of this rig. Attach the `WarTableDirector` script to the Rig to handle the fall-in and rise-out movements.
3.  **Light the Ford:** Create an empty GameObject named `Lighting_Rig`. Inside it, create two Directional Lights. Name the first `Sun_Raking`, set its color to a warm orange (#FF9933), intensity to 0.8, and rotation to X:15, Y:-45, Z:0. Name the second `Fill_Ambient`, set its color to a dim blue (#445566), intensity to 0.2, and rotation to X:75, Y:135, Z:0. Ensure no light is colored pure red to comply with the Stillness anti-strobe mandate.
4.  **Lay the Earth:** Create an empty GameObject named `Environment_SunderedFord`. Drag in the `Env_DiggableEarth` prefab and set its position to X:-10, Y:0, Z:0. Drag in the `Env_HighGroundRock` prefab and set its position to X:10, Y:2, Z:0. Drag in the `Env_RiverBed` prefab between them at X:0, Y:-0.5, Z:0 and assign it the `Mat_FrozenWater` material to establish the Ice magic presence.
5.  **Muster the Forces:** Create an empty GameObject named `Armies_PhaseII`. Drag the Grogen Champion prefab to X:-5, Y:0, Z:-2. Drag the Daminari Champion prefab to X:5, Y:2, Z:2. Drag the Dragon prefab high above the river at X:0, Y:15, Z:5. 
6.  **Bind the Armoury (Real SKUs):** Locate the empty Weapon Sockets on both Champion prefabs. Drag the exact, real 3D models of the SKUs sold in the armoury into these sockets. Apply the `Mat_ArmourySteel` material to these blades, ensuring Metallic is set to 1.0 and Smoothness to 0.9 so they catch the raking light and stand as the brightest objects on the field.
7.  **Draft the UI:** Create a UI Canvas. At the top center, place a Text element explicitly stating "THE WARBORN MARCH - FREE TRIAL SLICE". On the bottom left, place the Strike Panel containing four separate UI bars: Vigour, Might, Guard, and Magical. On the bottom right, place the Return Current Panel, configured to display the 3D model of the winning SKU and its rising offer upon battle completion.
8.  **Final Stillness Check:** Press Play in the editor. Verify the camera rests at the War Table position. Verify the Magical meter drains to zero, the Dragon departs on time, the Return Current triggers, and the StillnessGovernor logs no loops exceeding 3 Hz.