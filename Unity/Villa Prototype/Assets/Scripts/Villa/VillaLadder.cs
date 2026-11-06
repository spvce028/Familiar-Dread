using System.Collections.Generic;
using UnityEngine;

namespace FamiliarDread
{

    /// <summary>Beschreibt Leiterzugänge und animiert die Dachbodenluke. Die Bewegung übernimmt VillaExplorer.</summary>
    public class VillaLadder : MonoBehaviour
    {
        // Aktive Leitern registrieren, damit der Spieler sie nicht in jedem Bild in der ganzen Szene suchen muss.
        public static readonly List<VillaLadder> Active = new List<VillaLadder>();

        // Inspector-Referenzen: Austrittspunkte auf den Böden, Kletterpunkte an der Leiter und Scharnier der Luke.
        public Transform lowerExit, upperExit, lowerClimb, upperClimb, hatchPivot;

        // Bleibt auch bei geöffneter Luke aktiv, damit sie weiterhin mit E anvisiert werden kann.
        public Collider hatchCollider;

        // Gewünschter Zustand; während der Animation kann die tatsächliche Drehung noch davon abweichen.
        [SerializeField]
        bool open;
        Quaternion closedRotation;
        bool initialized;

        /// <summary>Gewünschter Öffnungszustand der Luke.</summary>
        public bool IsOpen
        {
            get
            {
                return open;
            }
        }

        void Awake()
        {
            Initialize();
        }

        // Nur einmal eintragen; beim Deaktivieren wieder aus der gemeinsamen Liste entfernen.
        void OnEnable()
        {
            Initialize();
            if (!Active.Contains(this))
                Active.Add(this);
        }

        void OnDisable()
        {
            Active.Remove(this);
        }

        // Einmalig die geschlossene Scharnierstellung merken. Mehrere Aufrufer dürfen die Initialisierung anstoßen.
        void Initialize()
        {
            if (initialized)
                return;
            if (hatchPivot)
                closedRotation = hatchPivot.localRotation;
            initialized = true;
            if (hatchCollider)
                hatchCollider.enabled = true;
        }

        void Update()
        {
            TickHatch(Time.deltaTime);
        }

        /// <summary>Dreht die Luke mit 140 Grad pro Sekunde zwischen geschlossen und 85 Grad geöffnet.</summary>
        public void TickHatch(float dt)
        {
            Initialize();
            if (hatchPivot)
                hatchPivot.localRotation = Quaternion.RotateTowards(hatchPivot.localRotation,
                    closedRotation * Quaternion.Euler(0, 0, open ? 85 : 0), 140 * Mathf.Max(0,
                    dt));
            if (hatchCollider)
                hatchCollider.enabled = true;
        }

        /// <summary>Berechnet den Mittelpunkt der geschlossenen Luke, auch wenn sie gerade offen steht.</summary>
        public Vector3 ClosedHatchCenter
        {
            get
            {
                Initialize();
                var box = hatchCollider as BoxCollider;
                if (!box || !hatchPivot)
                    return transform.position;
                var rotation = hatchPivot.parent.rotation * closedRotation;
                return hatchPivot.position + rotation * (box.transform.localPosition + box.transform.localRotation * Vector3.Scale(box.center,
                    box.transform.localScale));
            }
        }

        /// <summary>Prüft, ob der getroffene Collider zum Luken-Scharnier oder einem seiner Kinder gehört.</summary>
        public bool IsHatch(Collider target)
        {
            return target && hatchPivot && (target.transform == hatchPivot || target.transform.IsChildOf(hatchPivot));
        }

        /// <summary>Wechselt den Zustand; verhindert das Schließen, solange eine Figur im Durchstieg steht.</summary>
        public bool TryToggleHatch()
        {
            Initialize();
            if (open)
            {
                var box = hatchCollider as BoxCollider;
                if (box)
                {
                    // Den geschlossenen Lukenbereich um eine kleine Sicherheitszugabe erweitern.
                    Vector3 half = Vector3.Scale(box.size * .5f, box.transform.lossyScale) + new Vector3(.03f,
                        .12f, .03f);
                    var rotation = hatchPivot.parent.rotation * closedRotation * box.transform.localRotation;
                    var inverse = Quaternion.Inverse(rotation);
                    var origin = ClosedHatchCenter;

                    // Aktuelle Controller-Transforms statt Physics.OverlapBox verwenden: Physik-Proxys können nach Move noch veraltet sein.
                    foreach (var body in Object.FindObjectsByType<CharacterController>(FindObjectsSortMode.None))
                    {
                        if (!body.enabled || !body.gameObject.activeInHierarchy)
                            continue;
                        var scale = body.transform.lossyScale;

                        // Den CharacterController als Kapsel aus Radius und geradem Mittelstück beschreiben; Skalierung berücksichtigen.
                        float radius = body.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
                        float segment = Mathf.Max(0, body.height * Mathf.Abs(scale.y) * .5f - radius);
                        var center = body.transform.TransformPoint(body.center);
                        var axis = body.transform.up * segment;

                        // Beide Kapselenden ins Koordinatensystem der geschlossenen Luke umrechnen.
                        var a = inverse * (center - axis - origin);
                        var b = inverse * (center + axis - origin);

                        // Konservativer Überlappungstest mit den Kapselgrenzen: Im Zweifel bleibt die Luke offen.
                        var low = Vector3.Min(a, b) - Vector3.one * radius;
                        var high = Vector3.Max(a, b) + Vector3.one * radius;
                        if (low.x <= half.x
                            && high.x >= -half.x
                            && low.y <= half.y
                            && high.y >= -half.y
                            && low.z <= half.z
                            && high.z >= -half.z)
                            return false;
                    }
                }
            }

            open = !open;
            return true;
        }

        /// <summary>Prüft Nähe und W/S-Eingabe für den Einstieg an einem der beiden Leiterenden.</summary>
        /// <param name="feet">Weltposition der Spielfigur an den Füßen.</param>
        /// <param name="climbInput">W positiv für Aufstieg, S negativ für Abstieg; unabhängig vom Blick.</param>
        /// <param name="fromTop">Wird true, wenn der Einstieg oben am Dachboden erfolgt.</param>
        public bool CanGrab(Vector3 feet, float climbInput, out bool fromTop)
        {
            fromTop = false;
            if (!lowerExit || !upperExit || !lowerClimb || !upperClimb)
                return false;

            // Außerhalb der Leiterhöhe oder zu weit seitlich ist kein Greifen möglich.
            if (feet.y < lowerExit.position.y - .15f
                || feet.y > upperExit.position.y + .3f
                || Mathf.Abs(feet.x - lowerClimb.position.x) > .52f)
                return false;

            // Auf dem Dachboden mit S einsteigen. Die Luke muss vorher mit E geöffnet werden.
            if (feet.y >= upperExit.position.y - .18f)
            {
                if (!open || climbInput >= -.1f)
                    return false;

                Vector3 distance = upperClimb.position - feet;
                distance.y = 0;
                // Nur von der offenen Austrittsseite greifen, nicht durch das Geländer hinter der Luke.
                Vector3 exitDirection = upperExit.position - upperClimb.position;
                exitDirection.y = 0;
                if (Vector3.Dot(feet - upperClimb.position, exitDirection) < 0)
                    return false;

                fromTop = true;
                return distance.magnitude < 1.4f;
            }

            // Unten nur mit W greifen. Eine Figur kann nicht mitten im Schacht neu einsteigen.
            if (feet.y > lowerExit.position.y + .15f || climbInput <= .1f)
                return false;

            // Diese Prototyp-Leiter ist entlang der Welt-Z-Achse ausgerichtet.
            float z = feet.z - lowerClimb.position.z;
            return z >= -.25f && z <= .3f;
        }
    }
}
