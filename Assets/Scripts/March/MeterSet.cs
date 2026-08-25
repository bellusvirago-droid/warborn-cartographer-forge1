using UnityEngine;

/*
 * ATTACHMENT:
 * Attach this script to the root GameObject of a Banner unit (e.g., Prefabs/Banners/GrogenBanner or DaminariBanner).
 * 
 * INSPECTOR FIELDS TO SET:
 * - Banner House: Select 'Grogen' or 'Daminari' to determine recovery curves at the Sundered Ford.
 * - Max Vigour: Set the physical health ceiling (e.g., 100).
 * - Max Might: Set the stamina/attack power ceiling (e.g., 100).
 * - Max Guard: Set the defensive posture ceiling (e.g., 100).
 * - Starting Magical: Set the absolute maximum capacity for Ice magic (e.g., 40).
 */

namespace WarbornMarch.PhaseII
{
    public enum HouseName
    {
        Grogen,
        Daminari
    }

    // Sealed by construction. The meters dictate combat flow to ensure players must eventually rely on the real SKU steel they carry.
    public sealed class MeterSet : MonoBehaviour
    {
        [Header("Banner Identity")]
        [Tooltip("Determines the recovery curve: Grogens are heavy/resolute, Daminari are quick.")]
        [SerializeField] private HouseName bannerHouse;

        [Header("Physical Limits")]
        [SerializeField] private float maxVigour = 100f;
        [SerializeField] private float maxMight = 100f;
        [SerializeField] private float maxGuard = 100f;

        [Header("Arcane Limit")]
        [Tooltip("This is locked at initialization. It can never be expanded by outside code.")]
        [SerializeField] private float startingMagical = 40f;

        // Internal states - no public setters exist. What cannot be written cannot be bought.
        private float _vigour;
        private float _might;
        private float _guard;
        private float _magical;

        // This backing field is assigned exactly once in Awake to guarantee the capacity ceiling cannot be raised.
        private float _sealedMaxMagical;

        // Public accessors strictly for the UI and the StrikeReckoner.
        public float Vigour => _vigour;
        public float Might => _might;
        public float Guard => _guard;
        public float Magical => _magical;

        private void Awake()
        {
            _vigour = maxVigour;
            _might = maxMight;
            _guard = maxGuard;

            // We seal the absolute maximum at birth. There is no method in this class to rewrite this limit.
            _sealedMaxMagical = startingMagical;
            _magical = _sealedMaxMagical;
        }

        /// <summary>
        /// Represents a turn or action taken to rest. This is the ONLY structural path to regain Magical reserves.
        /// </summary>
        /// <param name="restSeconds">The duration or weight of the rest phase.</param>
        public void Rest(float restSeconds)
        {
            // Prevent negative time inputs from being used as a backdoor drain.
            if (restSeconds <= 0f) return;

            float vigourRate, mightRate, guardRate;

            if (bannerHouse == HouseName.Grogen)
            {
                // The Deep Dig: Heavy and planted. Slow physical recovery, but they reinforce guard exceptionally well.
                vigourRate = 4f;
                mightRate = 5f;
                guardRate = 15f;
            }
            else
            {
                // The Legion: Quick and relentless. Rapid physical recovery, but standard guard resetting.
                vigourRate = 12f;
                mightRate = 14f;
                guardRate = 5f;
            }

            // Apply recovery curves bounded by their physical maximums.
            _vigour = Mathf.Min(maxVigour, _vigour + (vigourRate * restSeconds));
            _might = Mathf.Min(maxMight, _might + (mightRate * restSeconds));
            _guard = Mathf.Min(maxGuard, _guard + (guardRate * restSeconds));

            // The Magical meter refills uniformly across houses at a slow, constant trickle.
            // Because _sealedMaxMagical cannot be changed, there is no way to overcharge this via rest.
            float magicalRate = 2f;
            _magical = Mathf.Min(_sealedMaxMagical, _magical + (magicalRate * restSeconds));
        }

        /// <summary>
        /// Attempts to channel Ice, the sole live magic of Phase II. Drains the meter on success.
        /// </summary>
        /// <param name="cost">The exact amount of magical reserve required.</param>
        /// <returns>True if the cast succeeds, false if reserves are insufficient.</returns>
        public bool TryCastIce(float cost)
        {
            // We structurally prevent negative costs from functioning as a backdoor refill.
            if (cost <= 0f) return false;

            if (_magical < cost)
            {
                // Meter runs dry, forcing the player to fall back to the actual SKU steel they carry.
                return false;
            }

            // Deduct the cost. There is no other method in this class that modifies _magical besides Rest().
            _magical -= cost;
            return true;
        }

        /// <summary>
        /// Applies incoming damage to Vigour and Guard during the StrikeReckoner's phase.
        /// </summary>
        public void SufferStrike(float vigourDamage, float guardDamage)
        {
            // Negative damage is blocked to prevent accidental healing during combat calculations.
            if (vigourDamage > 0f) _vigour = Mathf.Max(0f, _vigour - vigourDamage);
            if (guardDamage > 0f) _guard = Mathf.Max(0f, _guard - guardDamage);
        }

        /// <summary>
        /// Consumes Might when swinging the carried Return Current weapon (the SKU).
        /// </summary>
        public bool TryExpendMight(float cost)
        {
            if (cost <= 0f || _might < cost) return false;
            _might -= cost;
            return true;
        }
    }
}
