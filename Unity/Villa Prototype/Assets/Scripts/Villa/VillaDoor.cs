using UnityEngine;

namespace FamiliarDread
{

    /// <summary>Animiert drehende Türen oder ausziehbare Schubladen. Die E-Eingabe verarbeitet VillaExplorer.</summary>
    public class VillaDoor : MonoBehaviour
    {
        // Inspector: Öffnungszustand und Öffnungswinkel. Das Vorzeichen von angle bestimmt die Drehrichtung.
        public bool open;
        public float angle = 95;

        // Normale Türen drehen sich; Schubladen folgen ihrem lokalen Auszugsweg.
        public enum MovementType { Rotate, Slide }

        [Header("Bewegungsart")]
        public MovementType movement = MovementType.Rotate;
        public Vector3 slideOffset = new Vector3(0, 0, .38f);
        [Min(.01f)] public float slideSpeed = .8f;

        [Tooltip("Bezeichnung im E-Hinweis, beispielsweise Tür oder Schublade.")]
        public string interactionName = "Tür";

        Vector3 closedPosition;

        // Geheimtüren verraten ihre Position nicht durch den E-Hinweis. Bedienung bleibt für den Prototyp möglich.
        public bool hideInteractionHint;

        // Ausgangsdrehung beim Spielstart; die Tür muss dafür in ihrer geschlossenen Stellung stehen.
        Quaternion closed;

        // Die geschlossene Stellung relativ zum übergeordneten Objekt merken.
        void Awake()
        {
            closed = transform.localRotation;
            closedPosition = transform.localPosition;
        }

        /// <summary>Wechselt den Zielzustand. Die eigentliche Drehung erfolgt anschließend in Update.</summary>
        public void Toggle(Vector3 playerPosition)
        {

            /* --- Falls die Öffnung der Tür immer vom Spieler weg gehen soll ---

            // Nur beim Öffnen eine Richtung bestimmen.
            if (!open)
            {
                // Geschlossene Türstellung in Weltkoordinaten.
                Quaternion closedWorldRotation = transform.parent
                    ? transform.parent.rotation * closed
                    : closed;

                Vector3 doorForward = closedWorldRotation * Vector3.forward;
                Vector3 toPlayer = playerPosition - transform.position;

                // Positiv oder negativ: Auf welcher Seite steht der Spieler?
                float side = Vector3.Dot(doorForward, toPlayer);

                // Winkelgröße behalten, Drehrichtung passend wählen.
                angle = Mathf.Abs(angle) * (side >= 0 ? 1 : -1);
            }

            */

            open = !open;
            var audio = GetComponent<VillaInteractionAudio>();
            if (audio)
                audio.Play(open);
        }

        // Mit 140 Grad pro Sekunde zur Zielstellung drehen, unabhängig von der Bildrate.
        void Update()
        {
            if (movement == MovementType.Slide)
            {
                // Offset und Ausgangsposition sind lokal: Auch gedrehte Möbel öffnen in ihrer eigenen Richtung.
                Vector3 target = closedPosition + (open ? slideOffset : Vector3.zero);
                transform.localPosition = Vector3.MoveTowards(
                    transform.localPosition, target, Mathf.Max(.01f, slideSpeed) * Time.deltaTime);
                return;
            }

            transform.localRotation = Quaternion.RotateTowards(transform.localRotation,
                closed * Quaternion.Euler(0, open ? angle : 0, 0), 140 * Time.deltaTime);
        }
    }
}
