### THE OWNED FOUNDATION
By the Founder's Purse, the house holds the structural vocabulary of the slice. We do not need to buy what is already ours:

*   **The Bodies & The Liveries:** *European Knights Pack 01* and *Modular Knights*. Handled headlessly via `Material.SetColor()` and mesh-swap scripts during the Muster to forge the Grogen and Daminari lines.
*   **The Melee & Locomotion:** *Kevin Iglesias Animations*. Humanoid-rigged, retargeted via `.ht` Unity HumanTemplate YAML bindings.
*   **The Blood & The Ice:** *Ultimate VFX Bundle*. Particle systems for strikes and the Ice magic, clamped by the Cartographer via a scriptable object overriding `main.maxParticles` and `main.simulationSpeed` to strictly obey the 3 Hz limit and the stillness setting.
*   **The Ford's Bones:** *Medieval Castle Kit*. The ruined bridge and keep walls, laid out via programmatic grid coordinates.

### THE WANTING
To achieve the Founder's standing decree—a console-grade URP 6000.x presentation (cold steel, wet earth, bruised sky) that runs at 60fps in WebGL—the Purse is insufficient. The house lacks environmental rendering authority, the dragon contract's physical form, the specific physical SKUs of the real-world armoury, and the entire auditory dimension.

Here are the exact gaps, ranked.

#### RANK I: THE MUST-BUY
These are non-negotiable. Without them, the Phase II slice fails the acceptance law of Book Zero (missing dragon, silent impacts, or generic weapons).

**1. Dragon (HP)**
*   **Publisher:** MalberS Animations
*   **Price:** $64.99
*   **The Gap Filled:** The Phase II slice dictates "one hireable dragon contract." The Purse holds zero dragons. 
*   **If Missing:** The dragon departs on time with no path to betrayal, but it has no physical mesh to render. The slice fails.
*   **Headless Note:** Authored into the Ford via a serialized `.prefab` text file. Its breath weapon VFX will be clamped to prevent strobing.

**2. Real-World SKU Blade Scans (In-House)**
*   **Source:** Cartographer-authored / In-House Photogrammetry
*   **Price:** $0.00 (Time/Internal Cost)
*   **The Gap Filled:** The Return Current binds to a *real SKU*. We cannot use the generic swords from *Modular Knights*. 
*   **If Missing:** A player tests a generic stand-in, violating the Wall: "A player always tests the SAME EXACT PIECE that is sold in the armory."

**3. Poly Haven PBR Environment Maps (Mud, Ash, Cold Stone)**
*   **Source:** Poly Haven (CC0)
*   **Price:** $0.00
*   **The Gap Filled:** The *Medieval Castle Kit* provides geometry, but its default textures do not support the "wet earth / bruised sky" PBR mandate. We require high-fidelity Albedo/Normal/ORM (Occlusion, Roughness, Metallic) maps for the diggable ground and the Sundered Ford.
*   **If Missing:** The ground reads as flat and hobbyist. The "mud" hex will not read as wet; the "stone" hex will lack depth.

**4. Poly Haven HDRI - Stormy/Bruised Sky**
*   **Source:** Poly Haven (CC0)
*   **Price:** $0.00
*   **The Gap Filled:** Unity's default procedural skybox is too bright and generic. We need a specific "bruised sky" HDRI to drive the baked Global Illumination and Reflection Probes.
*   **If Missing:** The lighting reads as a sunny tech-demo, destroying the mournful, epic palette.

**5. Soniss GDC Audio Archive & Freesound.org Curated Curation**
*   **Source:** Soniss / Freesound (CC0 / Attribution)
*   **Price:** $0.00
*   **The Gap Filled:** The Purse has *zero* audio. We require steel-on-steel clashes, shield blocks, wet mud footsteps, and the shattering of Ice magic.
*   **If Missing:** The Ford is a silent movie. The Strike reckoning has no auditory feedback.

#### RANK II: RAISE IT FURTHER
These elevate the Ford from "acceptable modern PBR" to a highly polished, atmospheric piece, specifically targeting the requested mood.

**6. Volumetric Fog & Mist 2**
*   **Publisher:** Kronnect
*   **Price:** $49.00
*   **The Gap Filled:** While Unity 6 URP has built-in volumetric fog, Kronnect's solution allows for highly performant, stylized, height-based rolling mist over the river that scales gracefully down to the WebGL tier without killing the 60fps budget.
*   **If Missing:** We rely on standard URP fog, which is physically accurate but less dramatic for the "bruised and cold" art direction.

**7. Melee Weapons Pack (Audio)**
*   **Publisher:** SwishSwoosh
*   **Price:** $20.00
*   **The Gap Filled:** CC0 audio requires heavy equalisation and layering to sound "console-grade." A dedicated premium combat foley pack guarantees punchy, consistent low-end weight for the Strike reckoning.
*   **If Missing:** The Cartographer must script dynamic pitch/volume modulation over raw CC0 files to prevent ear fatigue. It will work, but lacks AAA punch.

#### RANK III: VANITY
Assets that would look incredible on the desktop tier but risk blowing out the WebGL frame budget or violating the stillness constraints.

**8. KWS Water System**
*   **Publisher:** Kripto289
*   **Price:** $59.99
*   **The Gap Filled:** Advanced fluid dynamics, shoreline foam, and true reflections for the broken river crossing.
*   **If Missing:** We use Unity 6's native URP Water system. It is slightly less spectacular but infinitely safer for WebGL performance and guarantees no looping/strobing shader artifacts that might violate the 3 Hz Wall.

---

### HEADLESS APPLICATION DECREE
No human will open the Unity Editor to import these. The Cartographer will author `AssetImportPipeline.cs`, an `AssetPostprocessor` that mounts the Poly Haven HDRIs, auto-generates the URP `.mat` files, assigns the Albedo/Normal/ORM maps, and generates the `Dragon.prefab` YAML with the MalberS mesh and the *Ultimate VFX* attached, serialized directly into the repository.

### TOTAL FOR THE MUST-BUY RANK
**$64.99** (One premium dragon; all other must-buy gaps are filled by in-house scanning and CC0 PBR/Audio archives).