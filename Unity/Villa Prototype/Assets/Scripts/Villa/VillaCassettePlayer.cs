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
            // Die Musik startet ausschließlich durch Interaktion, niemals beim Spielstart.
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

            if (!audioClip || !Source.enabled)
                return false;

            Source.clip = audioClip;
            Source.Play();
            return true;
        }

        void OnDisable()
        {
            Source.Stop();
        }
    }
}
