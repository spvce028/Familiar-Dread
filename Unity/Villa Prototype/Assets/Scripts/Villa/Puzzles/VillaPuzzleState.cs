using System.Globalization;
using System.Text;
using UnityEngine;

namespace FamiliarDread
{
    /// <summary>Daten und Fortschritt der ersten Rätselkette. Ein neuer Spielstart setzt den Fortschritt zurück.</summary>
    public class VillaPuzzleState : MonoBehaviour
    {
        public VillaPuzzleUI ui;

        [Header("Passwort und Hinweis")]
        public string password = "Rügen2012";
        public string[] passwordAliases = { "Ruegen2012" };
        public string passwordFormat = "Ort + Jahr · ohne Leerzeichen";
        [TextArea(3, 8)] public string noteText =
            "Passwort schon wieder vergessen?\n\nUnser erster gemeinsamer Urlaub. " +
            "Der Ort und das Jahr – genau wie auf dem Foto im Elternzimmer.\n\nOrt zuerst, Jahr dahinter.";

        [Header("Familienfoto")]
        public Texture2D photo;
        public string photoCaption = "Unser erster gemeinsamer Urlaub\nRügen · Sommer 2012";

        [Header("E-Mail")]
        public string mailSubject = "Die alte Aufnahme";
        public string mailSender = "Papa";
        [TextArea(5, 12)] public string mailBody =
            "Ich habe die alte Kassette beim Aufräumen wiedergefunden.\n\n" +
            "Sie liegt im Dachboden in der Kiste mit der blauen Markierung: UMZUG · 2016. " +
            "Nicht bei den Wintersachen.\n\n" +
            "Der Kassettenrekorder steht im dritten Schlafzimmer. Hör dir die Aufnahme an. " +
            "Vielleicht verstehst du dann, warum ich die Rückwand im Keller nie fertig gestrichen habe.\n\nPapa";

        [Header("Kassette")]
        public AudioClip tapeAudio;
        [TextArea(5, 12)] public string tapeTranscript =
            "Isaac, wenn du diese Aufnahme hörst, bin ich vermutlich nicht mehr im Haus. " +
            "Mit deiner Mutter stimmt etwas nicht. Vertrau ihr nicht einfach, nur weil sie deine Stimme kennt. " +
            "Die Rückwand im Keller ist nicht überall gleich dick. Such nach dem frisch gestrichenen Stück. " +
            "Der Weg dahinter führt zum Gartenhaus. Sei leise.";

        public bool NoteRead { get; private set; }
        public bool PhotoRead { get; private set; }
        public bool PcUnlocked { get; private set; }
        public bool MailRead { get; private set; }
        public bool TapeCollected { get; private set; }
        public bool TapePlayed { get; private set; }

        public void MarkNoteRead() { NoteRead = true; }
        public void MarkPhotoRead() { PhotoRead = true; }

        /// <summary>Groß-/Kleinschreibung, Leerzeichen und Umlautschreibweisen sollen kein Tastaturrätsel sein.</summary>
        public bool TryUnlock(string input)
        {
            string candidate = NormalizePassword(input);
            if (candidate.Length == 0)
                return false;
            if (candidate == NormalizePassword(password))
                return PcUnlocked = true;
            foreach (string alias in passwordAliases)
                if (!string.IsNullOrWhiteSpace(alias) && candidate == NormalizePassword(alias))
                    return PcUnlocked = true;
            return false;
        }

        static string NormalizePassword(string value)
        {
            var result = new StringBuilder();
            foreach (char c in (value ?? "").Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c))
                    result.Append(char.ToUpperInvariant(c));
            return result.ToString();
        }

        public bool ReadMail()
        {
            if (!PcUnlocked)
                return false;
            MailRead = true;
            return true;
        }

        public bool CollectTape()
        {
            if (TapeCollected)
                return false;
            TapeCollected = true;
            return true;
        }

        public void MarkTapePlayed() { TapePlayed = true; }
    }
}
