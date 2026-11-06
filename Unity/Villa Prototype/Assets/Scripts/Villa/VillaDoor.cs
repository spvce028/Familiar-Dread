using UnityEngine;

namespace FamiliarDread
{

    /// <summary>Animiert eine Tür um den Scharnierpunkt dieses Objekts. Die E-Eingabe verarbeitet VillaExplorer.</summary>
    public class VillaDoor : MonoBehaviour
    {
        // Inspector: Öffnungszustand und Öffnungswinkel. Das Vorzeichen von angle bestimmt die Drehrichtung.
        public bool open;
        public float angle = 95;

        // Geheimtüren verraten ihre Position nicht durch den E-Hinweis. Bedienung bleibt für den Prototyp möglich.
        public bool hideInteractionHint;

        // Ausgangsdrehung beim Spielstart; die Tür muss dafür in ihrer geschlossenen Stellung stehen.
        Quaternion closed;

        // Die geschlossene Stellung relativ zum übergeordneten Objekt merken.
        void Awake()
        {
            closed = transform.localRotation;
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
        }

        // Mit 140 Grad pro Sekunde zur Zielstellung drehen, unabhängig von der Bildrate.
        void Update()
        {
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation,
                closed * Quaternion.Euler(0, open ? angle : 0, 0), 140 * Time.deltaTime);
        }
    }
}
