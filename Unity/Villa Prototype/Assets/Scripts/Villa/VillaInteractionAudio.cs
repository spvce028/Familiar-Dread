using UnityEngine;

namespace FamiliarDread
{
    /// <summary>Gemeinsame räumliche Geräuschwiedergabe für Türen, Luken und Lichtschalter.</summary>
    /// <remarks>Diese Komponente nur an Objekten mit Geräusch anbringen; Möbel bleiben ohne sie stumm.</remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class VillaInteractionAudio : MonoBehaviour
    {
        [Tooltip("Geräusch beim Öffnen beziehungsweise Einschalten.")]
        public AudioClip activateClip;

        [Tooltip("Geräusch beim Schließen beziehungsweise Ausschalten.")]
        public AudioClip deactivateClip;

        AudioSource audioSource;

        AudioSource Source
        {
            get
            {
                if (!audioSource)
                    audioSource = GetComponent<AudioSource>();
                return audioSource;
            }
        }

        // Vorgaben beim Hinzufügen; Lautstärke und Hörweite bleiben am AudioSource einstellbar.
        void Reset()
        {
            Source.playOnAwake = false;
            Source.loop = false;
            Source.spatialBlend = 1;
            Source.minDistance = 1;
            Source.maxDistance = 12;
            Source.dopplerLevel = 0;
            Source.volume = .7f;
        }

        void Awake()
        {
            Source.playOnAwake = false;
        }

        /// <summary>Spielt den zum neuen Zustand passenden Clip genau einmal ab.</summary>
        public void Play(bool activated)
        {
            var clip = activated ? activateClip : deactivateClip;
            if (!clip || !Source.enabled)
                return;

            // Beim schnellen Umkehren nicht Öffnen und Schließen übereinander abspielen.
            Source.Stop();
            Source.PlayOneShot(clip);
        }

        void OnDisable()
        {
            if (audioSource)
                audioSource.Stop();
        }
    }
}
