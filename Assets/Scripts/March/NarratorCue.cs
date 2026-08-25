/*
ATTACH TO: A dedicated 'NarratorManager' GameObject in the Phase II Sundered Ford scene.
INSPECTOR FIELDS:
- VoiceSource: An AudioSource component for playing the narrator's voice (2D, no spatial blend).
- SubtitleDisplay: A reference to the UI Text / TextMeshProUGUI component for subtitles.
- CueTable: Array of 7 cues mapping NarratorEvent enum to AudioClip and text lines.
- RespectStillness: Boolean linking to the global stillness setting (if true, fades text without motion).
*/

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class NarratorCue : MonoBehaviour
{
    public enum NarratorEvent
    {
        Opening,
        FirstBlood,
        IceUnleashed,
        DragonPaid,
        Victory,
        VictoryAtCost,
        Defeat
    }

    [System.Serializable]
    public struct CueData
    {
        [Tooltip("The specific battle state that triggers this line.")]
        public NarratorEvent stateTrigger;
        [Tooltip("The spoken audio file. Must be plain and mournful.")]
        public AudioClip voiceClip;
        [TextArea(2, 4)]
        [Tooltip("The exact script of what is spoken. Never explains rules.")]
        public string subtitle;
    }

    [Header("Dependencies")]
    public AudioSource voiceSource;
    public Text subtitleDisplay; // Can be swapped to TMPro.TextMeshProUGUI by the UI architect

    [Header("The Script")]
    [Tooltip("Map all 7 Phase II events here.")]
    public CueData[] cueTable;

    [Header("Settings")]
    public bool respectStillness = true;

    // Tracking to ensure a state-driven cue isn't re-fired accidentally by the state machine
    private bool[] hasPlayed = new bool[7];

    // ------------------------------------------------------------------------
    // PHASE II SCRIPT REFERENCE (To be entered in the Inspector):
    // ------------------------------------------------------------------------
    // Opening:       "The Sundered Ford. The Grogens dig the mud of the west bank. The Daminari hold the eastern stones. We leave only ghosts in the water."
    // FirstBlood:    "Steel finds flesh. A brother does not go home."
    // IceUnleashed:  "The river freezes. It does not care whose blood it binds."
    // DragonPaid:    "A winged shadow falls. The contract is paid in ash."
    // Victory:       "The Legion breaks. The crossing is yours. Count the dead, and ask if it was worth it."
    // VictoryAtCost: "You hold the high ground. But the silence is too loud. You have nothing left to give."
    // Defeat:        "The line collapsed. We belong to the river now."
    // ------------------------------------------------------------------------

    /// <summary>
    /// Fired exclusively by game state changes (e.g., from StrikeReckoner or BattleManager).
    /// Never fired on a timer.
    /// </summary>
    public void TriggerCue(NarratorEvent eventType)
    {
        int eventIndex = (int)eventType;

        // The narrator never repeats a mournful observation in a single battle slice.
        if (hasPlayed[eventIndex]) return;

        CueData? foundCue = null;
        foreach (CueData cue in cueTable)
        {
            if (cue.stateTrigger == eventType)
            {
                foundCue = cue;
                break;
            }
        }

        if (foundCue.HasValue)
        {
            hasPlayed[eventIndex] = true;
            StopAllCoroutines(); // Interrupts previous subtitle to maintain strict alignment with current state
            StartCoroutine(PlayCue(foundCue.Value));
        }
    }

    private IEnumerator PlayCue(CueData data)
    {
        if (voiceSource != null && data.voiceClip != null)
        {
            voiceSource.Stop();
            voiceSource.clip = data.voiceClip;
            voiceSource.Play();
        }

        if (subtitleDisplay != null)
        {
            subtitleDisplay.text = data.subtitle;
            
            // Visual display rules: No strobing, no rapid loops. 
            // If stillness is required, we simply show/hide without alpha fading.
            if (!respectStillness)
            {
                yield return StartCoroutine(FadeText(0f, 1f, 0.5f));
            }
            else
            {
                Color c = subtitleDisplay.color;
                c.a = 1f;
                subtitleDisplay.color = c;
            }
        }

        // Wait for the clip to finish, or a default 4 seconds if no clip is attached
        float waitTime = data.voiceClip != null ? data.voiceClip.length : 4f;
        yield return new WaitForSeconds(waitTime);

        if (subtitleDisplay != null)
        {
            if (!respectStillness)
            {
                yield return StartCoroutine(FadeText(1f, 0f, 1f));
            }
            else
            {
                subtitleDisplay.text = "";
            }
        }
    }

    private IEnumerator FadeText(float startAlpha, float endAlpha, float duration)
    {
        float elapsed = 0f;
        Color c = subtitleDisplay.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            subtitleDisplay.color = c;
            yield return null;
        }

        c.a = endAlpha;
        subtitleDisplay.color = c;
    }
}
