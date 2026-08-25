# Asset Roll: The Sundered Ford (Phase II)

This roll governs every asset required to build the Grogens vs. Daminari vertical slice, ensuring perfect alignment with the Survey's rendering constants, the patent-locked mechanics, and the free-to-play mandate. 

## 1. The Armoury (The Return Current)
*These assets are the binding to the real world. They may never be generic stand-ins.*

*   **The Bound SKUs (Swords, Axes, Spears)**
    *   **Source:** **[MADE IN-HOUSE]** (3D scanned or modeled directly from the armourer's exact CAD/specs).
    *   **Licence:** Owned / Proprietary.
    *   **Cost:** High (Time/Labor). 
    *   **Note:** The exact piece sold must be the exact piece tested. Material setups must use the Survey's `Surface` shapes (`roughness`, `metalness`) to match real-world steel and leather.

## 2. The Houses (Grogens & Daminari)
*The figures must obey the exact girth, height, and stoop constants from the Survey (e.g., `blade`: 0.195 girth, 0.54 height, 0.02 stoop).*

*   **Base Figure Meshes (6 Classes)**
    *   **Source:** **[MADE IN-HOUSE]**
    *   **Licence:** Owned.
    *   **Cost:** Medium.
    *   **Note:** While stores sell figures, they do not sell models mapped perfectly to our 6 fixed mechanical rigs (`blade`, `spear`, `axe`, `bow`, `shield`, `hooded`).
*   **House Banners (Deep Dig & The Legion)**
    *   **Source:** **[MADE IN-HOUSE]**
    *   **Licence:** Owned.
    *   **Cost:** Low (2D Texture/Cloth sim setup).
    *   **Note:** Colors must strictly use `SIDE_CLOTH_OURS` (`#a87b2c`) and `SIDE_CLOTH_THEIRS` (`#7a2420`).
*   **Figure Animations (Idle, March, Recoil, Corpse Fall)**
    *   **Source:** Mixamo / Unity Asset Store (e.g., Kevin Iglesias Melee Animations), highly modified **[IN-HOUSE]** to match Survey algorithms.
    *   **Licence:** Standard Asset Store / Mixamo terms.
    *   **Cost:** ~$30 - $50 for base packs + Internal Labor.
    *   **Note:** The Survey dictates exact procedural overwrites for these (e.g., `Body Recoil` using Math.sin). Animations must blend flawlessly with procedural code.

## 3. The Ground & The War Table
*The board is locked to `COLS=9`, `ROWS=7`. The trial is always free.*

*   **Hexagonal Terrain Textures (Mud, Stone, Ash, Briar, Barrow)**
    *   **Source:** PolyHaven / Kenney.nl.
    *   **Licence:** CC0.
    *   **Cost:** Free.
    *   **Note:** Albedo textures must be tinted in Unity to perfectly match the Survey's hex constants (e.g., Ash `face` `#2e2a27`, `edge` `#423c37`).
*   **The Broken River (Water Shader)**
    *   **Source:** Unity Asset Store (e.g., Stylized Water 2 by Staggart Creations).
    *   **Licence:** Standard Asset Store.
    *   **Cost:** ~$20.
    *   **Note:** Must remain still enough to not violate the < 3Hz looping limit.
*   **The War Table & Background Environment**
    *   **Source:** **[MADE IN-HOUSE]**
    *   **Licence:** Owned.
    *   **Cost:** Medium.
    *   **Note:** The framing must obey the `Fit The Ground` algorithmic camera bounds. 

## 4. Magic, VFX, & The Contract
*Rule: No looping light faster than 3 Hz, no strobing red. Every animation must obey a stillness setting.*

*   **Ice Magic (The Live Magic)**
    *   **Source:** Unity Asset Store (e.g., Epic Toon VFX) + **[IN-HOUSE]** tuning.
    *   **Licence:** Standard Asset Store.
    *   **Cost:** ~$35.
    *   **Note:** Must fade linearly; absolutely no high-frequency strobing or flashing pulses.
*   **Strike Theatre (Sparks, Flare, Shock Ring)**
    *   **Source:** Kenney.nl (Particle Pack) driven by **[IN-HOUSE]** algorithmic code.
    *   **Licence:** CC0.
    *   **Cost:** Free.
    *   **Note:** Visuals must strictly follow the Survey's `Strike Theatre` math (weights, life limits, hex colours like `#f0cf7a` for clean bands).
*   **The Dragon (Hireable Contract)**
    *   **Source:** Unity Asset Store (e.g., Malbers Animations - Poly Art Dragon) or Sketchfab.
    *   **Licence:** Standard Asset Store / Standard 3D Licence.
    *   **Cost:** ~$30 - $60.
    *   **Note:** Needs arrival, idle, and departure animations. Dragon logic is sealed; no path to betrayal.

## 5. The Soundscape
*   **The Bagpipe-and-Drum Spine (Dynamic Music)**
    *   **Source:** **[MADE IN-HOUSE]** / Contracted Composer.
    *   **Licence:** Owned (Work-for-hire).
    *   **Cost:** High.
    *   **Note:** Must be authored in stems (Drums, Pipes, Drone) so it can swell algorithmically during the turn and fall into absolute silence for the close (per the `WON_LINES` logic: *"the quiet comes in behind it like weather"*).
*   **The Narrator's Voice**
    *   **Source:** **[MADE IN-HOUSE]** (Voice Actor).
    *   **Licence:** Owned (Buyout).
    *   **Cost:** Medium.
    *   **Note:** Must record the exact literal string arrays from the Survey (`WON_LINES`, `LOST_LINES`, `TELLING`, and Sergeant `ORDER` steps). Dry, weary delivery; no theatrics.
*   **Foley & UI (Steel, Ground Shudders, Crows, Bells)**
    *   **Source:** Freesound.org.
    *   **Licence:** CC0.
    *   **Cost:** Free (Labor to mix).
    *   **Note:** Must fulfill the exact Omens (Rain on old iron, bells, crows) and the heavy, unsheathed steel impact sounds dictated by the `weight` variable in the `Strike Theatre`.
