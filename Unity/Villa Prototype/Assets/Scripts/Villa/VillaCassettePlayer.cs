using UnityEngine;

namespace FamiliarDread
{
    /// <summary>Spielt die eingelegte Kassette per E ab und stoppt sie bei erneuter Interaktion.</summary>
    /// <remarks>Die Eingabe, Reichweite und Hervorhebung übernimmt VillaExplorer.</remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class VillaCassettePlayer : MonoBehaviour
    {
        [Header("Kassette")]
        [Tooltip("Hier die importierte Musik- oder Sprachdatei aus dem Project-Fenster hineinziehen.")]
        public AudioClip audioClip;

        [Tooltip("Optional: Verknüpft diesen Rekorder mit der gefundenen Rätselkassette.")]
        public VillaPuzzleState puzzle;
        bool originalLoop;

        public string InteractionHint
        {
            get
            {
                if (IsPlaying)
                    return "E · Wiedergabe stoppen";
                return puzzle && !puzzle.TapeCollected ? "E · Kassettenrekorder benutzen"
                    : puzzle ? "E · Kassette abspielen" : "E · Musik abspielen";
            }
        }

        public string UnavailableMessage
        {
            get
            {
                if (puzzle && !puzzle.TapeCollected)
                    return "Keine Kassette eingelegt – finde zuerst die Aufnahme.";
                if (puzzle ? !puzzle.tapeAudio : !audioClip)
                    return "Keine Aufnahme zugewiesen – Audio Clip im Inspector prüfen.";
                return "AudioSource am Kassettenrekorder ist deaktiviert";
            }
        }

        AudioSource audioSource;

        // Die Referenz bei Bedarf holen: funktioniert auch vor Awake, beispielsweise im Editor.
        AudioSource Source
        {
            get
            {
                if (!audioSource)
                    audioSource = GetComponent<AudioSource>();
                return audioSource;
            }
        }

        /// <summary>Bestimmt den Text des Interaktionshinweises.</summary>
        public bool IsPlaying
        {
            get { return Source.isPlaying; }
        }

        // Vorgaben beim erstmaligen Hinzufügen. Lautstärke, Loop und Reichweite bleiben am AudioSource einstellbar.
        void Reset()
        {
            Source.playOnAwake = false;
            Source.loop = true;
            Source.spatialBlend = 1;
            Source.minDistance = 1;
            Source.maxDistance = 12;
            Source.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        void Awake()
        {
            originalLoop = Source.loop;
            Source.playOnAwake = false;
        }

        /// <summary>Startet oder stoppt die Wiedergabe. Ohne zugewiesene Datei wird false zurückgegeben.</summary>
        public bool TryToggle()
        {
            if (IsPlaying)
            {
                Source.Stop();
                return true;
            }

            // Die bisher zugewiesene Musik bleibt erhalten. Ein verknüpftes Rätsel nutzt seine eigene Kassette.
            var clip = puzzle ? (puzzle.TapeCollected ? puzzle.tapeAudio : null) : audioClip;
            if (!clip || !Source.enabled)
                return false;

            Source.clip = clip;
            Source.loop = puzzle ? false : originalLoop;
            Source.Play();
            if (puzzle)
            {
                puzzle.MarkTapePlayed();
                if (puzzle.ui)
                    puzzle.ui.ShowTranscript(this);
            }
            return true;
        }

        void OnDisable()
        {
            Source.Stop();
        }
    }
}
