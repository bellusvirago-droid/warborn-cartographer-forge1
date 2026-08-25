# The Muster at the Sundered Ford

## The Face-Down Placement
Every battle in the Vertical Slice begins at the Sundered Ford, a crossing defined by its strict asymmetry: high ground to the east, diggable earth to the west. Before the StrikeReckoner assumes control, the Grogens and the Daminari must commit their forces to the field. This phase is the Muster. Every unit is deployed onto the field as a face-down banner. The player commands their chosen house, deploying pieces armed with the exact real-world armory SKUs they are testing. In accordance with the unalterable laws of the March, these blades are played entirely free and are bound to the Return Current.

## Visibility and the Fog of Banners
A commander knows her own troops, but never the enemy's mind. During the Muster, the player enjoys perfect visibility of her own side. She sees the true face of every friendly unit she places, alongside their full stat weights for Vigour, Might, Guard, and Magical. 

Conversely, the enemy force is presented as a wall of uniform, undifferentiated hostile banners. There are no subtle hints in the interface, no scale variations in the shadows, and no distinct silhouettes to betray what lies beneath. An Ice magus looks identical to a frontline infantryman until the moment of truth.

## The Single Scout
The player is granted a single scout during the Muster. This creates an immediate tactical fulcrum. The player must choose: spend the scout to target exactly one face-down enemy banner, forcing it to reveal its unit type, weapon SKU, and stats ahead of the Strike; or hold the scout in reserve. Spending it grants localized intelligence that can dictate the final placement of the player's line. Holding it preserves a tactical advantage that will carry over into the Strike phase, at the cost of deploying entirely blind.

## The Resolution of the Reveal
When a scout is spent, or when the final lock initiates, a reveal resolves. The transition from a face-down generic banner to a revealed unit must be decisive, clean, and entirely compliant with the aesthetic laws of the house. It must obey the stillness settings perfectly: there shall be no looping light faster than 3 Hz, and absolutely no strobing red. The banner model dissolves or flips smoothly into the true unit model, and the UI updates instantly to display the revealed Vigour, Might, Guard, and Magical pools.

## The Law of Rendering and Data Starvation
It is a standing law of this architecture: **no face may ever reach the renderer before its reveal resolves.** 

We do not honor this law by hiding units behind camera culling masks, dropping their opacity to zero, or sinking them beneath the terrain. A hidden unit must not exist in the client's scene hierarchy. The design enforces total data starvation. The object instantiated on the board during placement is exclusively the generic banner prefab. The true unit data, its mesh, and its materials are deliberately withheld by the sealed architecture. Only when the server validates that a reveal has perfectly resolved does the system instantiate the true unit and destroy the proxy banner. There is absolutely nothing in the Unity scene for a headless client to datamine or a free-cam to peek at.

## The Ninety-Second Beat Sheet
The Muster is strictly timed to a ninety-second clock, ushering the player cleanly from the arrival to the Strike.

* **0:00 - 0:15 | The Arrival:** The Sundered Ford loads. The player's free trial SKUs are bound. The camera sweeps the broken river crossing, settling firmly on the deployment zones.
* **0:15 - 0:45 | The Placement:** The player drags their face-down banners onto their designated terrain. The enemy's generic banners populate the opposing bank simultaneously, governed by the opposing logic.
* **0:45 - 0:65 | The Scout's Window:** The player is prompted with the single scout. They have twenty seconds to select one enemy banner to reveal, or explicitly choose the prompt to hold the scout.
* **0:65 - 0:80 | The Adjustment:** If the scout was spent, the player uses this brief window to adjust their existing placements in response to the newly revealed enemy unit.
* **0:80 - 0:90 | The Lock and Reveal:** The board locks. Placements are finalized. All remaining face-down enemy banners flip in a synchronized, stillness-compliant sequence. The Muster ends, the UI clears, and control is handed seamlessly to the StrikeReckoner.