using UnityEngine;

/// <summary>
/// THE WARBORN MARCH - Phase II: The Vertical Slice
/// ATTACH TO: The physical "WarTable" root GameObject in the Sundered Ford scene.
/// 
/// REQUIRED IN INSPECTOR:
/// - RaycastCamera: The player's first-person or fixed table-view camera.
/// - DaggerOfCommencement: Collider on the physical dagger stabbed into the table.
/// - ParchmentMuster: Collider on the scroll where troops, Ice magic, and Dragon are allocated.
/// - GrogenFigure / DaminariFigure: Colliders on the carved wooden representation of the houses.
/// - CompassStillAir: Collider on the physical brass compass at the table's edge (Click 1).
/// - NeedleFreezeToggle: Collider on the compass needle itself, exposed when compass is open (Click 2).
/// - SkuMountRacks: The physical rests where the exact Return Current 3D model SKUs are seated.
/// </summary>
public class WarTableUI : MonoBehaviour
{
    [Header("Diegetic interactables")]
    public Camera raycastCamera;
    public Collider daggerOfCommencement;
    public Collider parchmentMuster;
    public Collider grogenFigure;
    public Collider daminariFigure;
    public Collider skuMountRacks;
    
    [Header("Still Air (Accessibility)")]
    public Collider compassStillAir; 
    public Collider needleFreezeToggle; 
    public Transform compassLid;

    [Header("Slice State")]
    public bool isBattleActive = false;
    private bool isCompassOpen = false;
    private bool stillAirEnforced = false;

    private void Update()
    {
        // We rely entirely on physical interactions with the table elements. 
        // No Canvas, no screen-space HUD, no floating rectangles.
        if (Input.GetMouseButtonDown(0))
        {
            HandleTableInteraction();
        }
    }

    private void HandleTableInteraction()
    {
        // Cast a ray from the camera through the mouse position into the 3D world
        Ray ray = raycastCamera.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // ========================================================================
            // STILL AIR FREEZE: Reachable in exactly two clicks from ANY state.
            // Click 1: The Compass on the edge of the table.
            // Click 2: The Needle inside the Compass.
            // ========================================================================
            if (hit.collider == compassStillAir)
            {
                OpenStillAirCompass();
                return; // Interaction consumed
            }
            
            if (isCompassOpen && hit.collider == needleFreezeToggle)
            {
                ToggleStillAir();
                return; // Interaction consumed
            }

            // Mid-battle, the rest of the table is locked except for the Still Air compass.
            if (isBattleActive) 
            {
                return; 
            }

            // ========================================================================
            // THE MUSTER & THE RETURN CURRENT
            // ========================================================================
            if (hit.collider == skuMountRacks)
            {
                // Inspect the EXACT piece from the armoury. No generic stand-ins permitted.
                // Triggers physical rotation of the blade mesh for inspection.
                InspectBoundSKU();
            }
            else if (hit.collider == grogenFigure || hit.collider == daminariFigure)
            {
                // Adjust troop positions between the Deep Dig (west) and high ground (east)
                // at the Sundered Ford.
                SelectFormation(hit.collider);
            }
            else if (hit.collider == parchmentMuster)
            {
                // Adjust Vigour, Might, Guard, Magical (Ice), and the single Dragon Contract.
                AdjustMusterLedger(hit.point);
            }
            else if (hit.collider == daggerOfCommencement)
            {
                // Stabbing the dagger deeper into the table locks the Muster and begins the Strike.
                CommenceStrike();
            }
        }
    }

    private void OpenStillAirCompass()
    {
        if (!isCompassOpen)
        {
            isCompassOpen = true;
            // Physically rotate the brass lid of the compass open on the table.
            // This exposes the needleFreezeToggle collider to raycasts.
            compassLid.localRotation = Quaternion.Euler(-180f, 0, 0);
        }
    }

    private void ToggleStillAir()
    {
        stillAirEnforced = !stillAirEnforced;
        
        // Move the needle physically to "Still" or "Flow" markings on the brass.
        float needleAngle = stillAirEnforced ? 45f : -45f;
        needleFreezeToggle.transform.localRotation = Quaternion.Euler(0, needleAngle, 0);

        // Enforce the Founder's accessibility rules globally:
        // - Kills all strobing red effects.
        // - Clamps all loop animations (banners, water at the Ford) to below 3 Hz.
        // - Freezes idle breathing animations on units.
        if (StillAir.Instance != null) StillAir.Instance.GlobalFreezeSwitch = stillAirEnforced;
    }

    private void InspectBoundSKU()
    {
        // Logic to physically elevate the real Return Current SKU from the rack.
        // Visual only; stats are already bound to the sealed StrikeReckoner.
        Debug.Log("Inspecting genuine armoury SKU.");
    }

    private void SelectFormation(Collider figureCollider)
    {
        // Highlight the wooden carved figure.
        // Logic maps to Grogens (Deep Dig) or Daminari (Legion).
        Debug.Log("Formation figure adjusted: " + figureCollider.name);
    }

    private void AdjustMusterLedger(Vector3 hitPoint)
    {
        // In a full implementation, we map the local hitPoint on the parchment 
        // to specific ink lines (Ice Magic allocation, Dragon Contract hiring).
        Debug.Log("Ink adjusts on the Muster parchment.");
    }

    private void CommenceStrike()
    {
        // Once the dagger is struck, the table UI locks down (except Still Air).
        // The view shifts to the Sundered Ford battle.
        isBattleActive = true;
        
        // Push animation: Dagger sinks into the wood.
        daggerOfCommencement.transform.position -= new Vector3(0, 0.05f, 0);

        // The Muster data is passed to the sealed StrikeReckoner class. 
        // We do not calculate combat here.
        Debug.Log("Dagger struck. Passing Muster to sealed StrikeReckoner.");
    }
}
