using UnityEngine;

namespace FamiliarDread
{
    /// <summary>Ein Blickziel der Rätselkette. Reichweite, E-Eingabe und Highlight übernimmt VillaExplorer.</summary>
    public class VillaPuzzleObject : MonoBehaviour
    {
        public enum Kind { Note, Photo, Computer, Crate, Tape, Flashlight }
        public Kind kind;
        public VillaPuzzleState story;
        public string title;

        [Tooltip("Spielfigur, die diese Taschenlampe beim Aufnehmen erhält.")]
        public VillaExplorer flashlightOwner;

        [Tooltip("Stärke der vorhandenen Highlightfarbe beim Anvisieren.")]
        [Range(0f, 1f)] public float highlightStrength = .30f;

        [Header("Optionaler eigener Notiztext")]
        [Tooltip("Leer lassen, um den zentralen Passwort-Hinweis zu verwenden.")]
        [TextArea(4, 12)] public string noteTextOverride;

        public string DocumentText
        {
            get { return string.IsNullOrWhiteSpace(noteTextOverride) ? story.noteText : noteTextOverride; }
        }
        [TextArea] public string emptyCrateText = "Nur alte Gegenstände. Keine Kassette.";

        [Header("Nur für Kisten")]
        public Transform lid;
        public GameObject tapeObject;
        Quaternion closedLid;
        bool crateOpen;

        [Header("Nur für den PC")]
        public Renderer computerScreen;
        MaterialPropertyBlock originalScreen;

        public bool IsCrateOpen { get { return crateOpen; } }

        public string InteractionHint
        {
            get
            {
                switch (kind)
                {
                    case Kind.Note: return "E · Notiz lesen";
                    case Kind.Photo: return "E · Familienfoto ansehen";
                    case Kind.Flashlight: return "E · Taschenlampe aufnehmen";
                    case Kind.Computer: return "E · Computer einschalten";
                    case Kind.Crate: return crateOpen ? "E · Kiste schließen" : "E · Kiste öffnen";
                    default: return "E · Kassette aufnehmen";
                }
            }
        }

        void Awake()
        {
            if (lid)
                closedLid = lid.localRotation;
            if (computerScreen)
            {
                originalScreen = new MaterialPropertyBlock();
                computerScreen.GetPropertyBlock(originalScreen);
            }
        }

        void Update()
        {
            if (kind == Kind.Crate && lid)
                lid.localRotation = Quaternion.RotateTowards(lid.localRotation,
                    closedLid * Quaternion.Euler(crateOpen ? -100 : 0, 0, 0), 140 * Time.deltaTime);
        }

        public bool TryInteract()
        {
            if (!isActiveAndEnabled || !story || !story.ui)
                return false;
            switch (kind)
            {
                case Kind.Note:
                    if (string.IsNullOrWhiteSpace(noteTextOverride))
                        story.MarkNoteRead();
                    story.ui.OpenDocument(this);
                    return true;
                case Kind.Photo:
                    story.MarkPhotoRead();
                    story.ui.OpenDocument(this);
                    return true;
                case Kind.Computer:
                    story.ui.OpenComputer(this);
                    return true;
                case Kind.Crate:
                    crateOpen = !crateOpen;
                    if (tapeObject)
                        tapeObject.SetActive(crateOpen && !story.TapeCollected);
                    if (crateOpen && (!tapeObject || story.TapeCollected) && !string.IsNullOrWhiteSpace(emptyCrateText))
                        story.ui.ShowToast(emptyCrateText);
                    return true;
                case Kind.Flashlight:
                    if (!flashlightOwner || !flashlightOwner.CollectFlashlight())
                        return false;
                    story.ui.ShowToast("Taschenlampe aufgenommen, zum benutzen F drücken");
                    gameObject.SetActive(false);
                    return true;
                case Kind.Tape:
                    if (!story.CollectTape())
                        return false;
                    story.ui.ShowToast("Kassette aufgenommen");
                    gameObject.SetActive(false);
                    return true;
            }
            return false;
        }

        /// <summary>Nur den Bildschirm individuell einfärben; das gemeinsam genutzte Material bleibt erhalten.</summary>
        public void SetComputerPowered(bool powered)
        {
            if (!computerScreen)
                return;
            if (!powered)
            {
                computerScreen.SetPropertyBlock(originalScreen != null && !originalScreen.isEmpty ? originalScreen : null);
                return;
            }
            var properties = new MaterialPropertyBlock();
            computerScreen.GetPropertyBlock(properties);
            properties.SetColor("_BaseColor", new Color(.16f, .3f, .25f));
            computerScreen.SetPropertyBlock(properties);
        }
    }
}
