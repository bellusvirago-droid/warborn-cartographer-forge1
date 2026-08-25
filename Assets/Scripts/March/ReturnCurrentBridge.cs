using UnityEngine;
using WarbornMarch.PhaseII;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

/// <summary>
/// THE WARBORN MARCH - Phase II: The Vertical Slice
/// Bridges the game board at the Sundered Ford to the physical armory.
/// STRICTLY READ-ONLY OUTWARD: It reports a victory for the equipped SKU so the 
/// external house may execute the Return Current rules (spoil-to-offer, 25% ceiling).
/// No pricing logic, discounts, or constants are ever computed or sent from this client.
/// </summary>
public class ReturnCurrentBridge : MonoBehaviour
{
    [Header("Armory Link Configuration")]
    [Tooltip("The exact SKU of the physical blade used in this trial. Must match the armory perfectly.")]
    [SerializeField] private string equippedBladeSKU;

    [Tooltip("The read-only webhook/endpoint that listens for victory events.")]
    [SerializeField] private string armoryEndpointURI;

    [Tooltip("Session token for the current free trial player. Used by the house to bind the offer.")]
    [SerializeField] private string sessionToken;

    [System.Serializable]
    private struct VictoryPayload
    {
        public string sku;
        public string eventType;
        public string session;
    }

    [Header("The Steel Carried")]
    [Tooltip("The exact armoury SKU the Grogen banner carries onto the ford.")]
    [SerializeField] private string grogenBladeSKU;

    [Tooltip("The exact armoury SKU the Daminari banner carries onto the ford.")]
    [SerializeField] private string daminariBladeSKU;

    /// <summary>
    /// Bound BEFORE the first blow: names which real ware the player carries.
    /// It only names the piece. No price, no offer, no ceiling is reckoned here —
    /// the house alone rules the Return Current.
    /// </summary>
    public void CarryTheSteel(HouseName house)
    {
        string sku = house == HouseName.Daminari ? daminariBladeSKU : grogenBladeSKU;
        if (string.IsNullOrEmpty(sku))
        {
            Debug.LogWarning($"[Return Current] No SKU is bound for {house}. The trial runs, but no ware is named.");
            return;
        }
        equippedBladeSKU = sku;
        Debug.Log($"[Return Current] {house} carries {sku} — the same exact piece the armoury sells.");
    }

    /// <summary>
    /// Called by the StrikeReckoner or BattleManager when the Grogens or Daminari achieve total victory.
    /// </summary>
    public void ReportVictory()
    {
        // We only report the victory of the exact SKU equipped on the board.
        // Validation: Ensure the SKU is not empty or a generic placeholder.
        if (string.IsNullOrEmpty(equippedBladeSKU) || equippedBladeSKU.Contains("GENERIC"))
        {
            Debug.LogError("[Return Current] Invalid SKU. The piece tested must be the exact piece sold in the armory.");
            return;
        }

        StartCoroutine(DispatchVictoryPayload(equippedBladeSKU, sessionToken));
    }

    private IEnumerator DispatchVictoryPayload(string sku, string session)
    {
        // Constructing a strict, read-only payload. 
        // Pricing, the 25% ceiling, and the Governor are locked externally by patent.
        VictoryPayload payload = new VictoryPayload
        {
            sku = sku,
            eventType = "SUNDERED_FORD_VICTORY",
            session = session
        };

        string jsonPayload = JsonUtility.ToJson(payload);

        using (UnityWebRequest request = new UnityWebRequest(armoryEndpointURI, "POST"))
        {
            // Encode the payload into a UTF8 byte array for the request body
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            
            // Set headers to explicitly define the JSON content type
            request.SetRequestHeader("Content-Type", "application/json");

            // Yield until the external house acknowledges the report
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                // Log failure, but do not interrupt the player's free trial experience.
                Debug.LogError($"[Return Current Bridge] Failed to report victory to the Armory: {request.error}");
            }
            else
            {
                // The event was successfully dispatched. The external house takes over entirely from here.
                Debug.Log($"[Return Current Bridge] Victory reported for SKU {sku}. The external house may now raise the time-bound offer.");
            }
        }
    }
}
