using System.Collections.Generic;
using UnityEngine;

/*
 * THE WARBORN MARCH - Phase II: The Vertical Slice
 * Location: The Sundered Ford
 * Faction: Daminari (The Legion Signature)
 * 
 * ATTACHMENT:
 * Attach to a singleton manager GameObject named 'System_DaminariLegion' in the scene.
 * 
 * INSPECTOR FIELDS:
 * - Formation Radius: The maximum world-space distance for two banners to lock shields.
 * 
 * CONSTRAINTS UPHELD:
 * - Reads MeterSet but NEVER overwrites base constants (StrikeReckoner receives an overlay).
 * - A broken line severs the graph; isolated pieces fall back to their base Guard.
 * - No strobing or looping light > 3Hz. Visuals (Gizmos) are static editor lines.
 */

public class LegionSystem : MonoBehaviour
{
    /// <summary>
    /// The one Legion of the field. A piece asks the Legion for its true Guard;
    /// it must be able to find it without an Inspector wire.
    /// </summary>
    public static LegionSystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[LegionSystem] A second Legion stood up. The later one stands down.");
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    [Header("Legion Constants")]
    [Tooltip("Maximum distance between two Daminari pieces to share Guard.")]
    public float formationRadius = 2.5f;

    // The master list of all live Daminari pieces currently on the Sundered Ford.
    private readonly List<LegionBanner> activeBanners = new List<LegionBanner>();

    // The resulting map of effective Guard pools. The StrikeReckoner queries this.
    // By isolating this map, we guarantee the base MeterSet constants are never altered.
    private readonly Dictionary<LegionBanner, int> effectiveGuardPool = new Dictionary<LegionBanner, int>();

    /// <summary>
    /// Called by a Daminari piece when it enters the field (Muster phase).
    /// </summary>
    public void RegisterBanner(LegionBanner banner)
    {
        if (!activeBanners.Contains(banner))
        {
            activeBanners.Add(banner);
            effectiveGuardPool[banner] = 0;
        }
    }

    /// <summary>
    /// Called by a Daminari piece when it dies or is exiled. Breaking the line.
    /// </summary>
    public void UnregisterBanner(LegionBanner banner)
    {
        if (activeBanners.Contains(banner))
        {
            activeBanners.Remove(banner);
            effectiveGuardPool.Remove(banner);
        }
    }

    /// <summary>
    /// The StrikeReckoner calls this to read the true Guard of a piece in combat.
    /// If the piece is alone, it returns its base Guard. If linked, it returns the Legion pool.
    /// </summary>
    public int GetEffectiveGuard(LegionBanner banner)
    {
        if (effectiveGuardPool.TryGetValue(banner, out int pooledGuard))
        {
            return pooledGuard;
        }
        
        // Fallback if unregistered, strictly reading the base constant.
        return banner.Meters != null ? banner.Meters.Guard : 0;
    }

    // We evaluate the line at the end of every frame to ensure any movement,
    // knockbacks from Ice magic, or deaths immediately shatter or form the line.
    private void LateUpdate()
    {
        RecalculateLegions();
    }

    /// <summary>
    /// Performs a deterministic graph traversal to find contiguous Daminari lines.
    /// A line shares the sum of its parts. A broken line leaves the pieces alone.
    /// </summary>
    private void RecalculateLegions()
    {
        HashSet<LegionBanner> visited = new HashSet<LegionBanner>();

        foreach (LegionBanner banner in activeBanners)
        {
            if (visited.Contains(banner)) 
                continue;

            // Start mapping a new contiguous line (Connected Component)
            List<LegionBanner> currentLine = new List<LegionBanner>();
            Queue<LegionBanner> queue = new Queue<LegionBanner>();

            queue.Enqueue(banner);
            visited.Add(banner);

            int totalLineGuard = 0;

            // Breadth-First Search to find all adjacent banners in this specific line
            while (queue.Count > 0)
            {
                LegionBanner current = queue.Dequeue();
                currentLine.Add(current);

                // Read from the sealed MeterSet. We never write back to it.
                if (current.Meters != null)
                {
                    totalLineGuard += current.Meters.Guard;
                }

                // Check adjacencies against the rest of the field
                foreach (LegionBanner other in activeBanners)
                {
                    if (!visited.Contains(other))
                    {
                        float sqrDistance = (current.transform.position - other.transform.position).sqrMagnitude;
                        if (sqrDistance <= (formationRadius * formationRadius))
                        {
                            visited.Add(other);
                            queue.Enqueue(other);
                        }
                    }
                }
            }

            // The reckoning: Apply the shared pool to the effective map.
            // If currentLine.Count == 1, totalLineGuard equals their own base Guard (they are alone).
            foreach (LegionBanner piece in currentLine)
            {
                effectiveGuardPool[piece] = totalLineGuard;
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Editor-only visualization of the Daminari lines at the Sundered Ford.
        // Color is a still, non-strobing Daminari Blue.
        Gizmos.color = new Color(0.1f, 0.4f, 0.8f, 1.0f);
        
        if (activeBanners == null) return;

        for (int i = 0; i < activeBanners.Count; i++)
        {
            for (int j = i + 1; j < activeBanners.Count; j++)
            {
                LegionBanner a = activeBanners[i];
                LegionBanner b = activeBanners[j];

                if (a != null && b != null)
                {
                    if (Vector3.Distance(a.transform.position, b.transform.position) <= formationRadius)
                    {
                        Gizmos.DrawLine(a.transform.position, b.transform.position);
                    }
                }
            }
        }
    }
}

/* 
 * ==========================================
 * DEPENDENCY STUBS FOR HEADLESS COMPILATION
 * ==========================================
 * These represent existing architecture from Book Zero.
 * Placed here so the file compiles flawlessly in a vacuum.
 */

public interface IMeterSet
{
    // Read-only contract for the reckoning.
    int Guard { get; }
}

public class LegionBanner : MonoBehaviour
{
    // In the real Unity build, this fetches the actual MeterSet component on the piece.
    private IMeterSet _meters;
    public IMeterSet Meters 
    {
        get 
        {
            if (_meters == null) _meters = GetComponent<IMeterSet>();
            return _meters;
        }
    }

    private void Start()
    {
        LegionSystem system = FindObjectOfType<LegionSystem>();
        if (system != null) system.RegisterBanner(this);
    }

    private void OnDestroy()
    {
        LegionSystem system = FindObjectOfType<LegionSystem>();
        if (system != null) system.UnregisterBanner(this);
    }
}
