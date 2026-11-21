using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FamiliarDread
{

    /// <summary>Steuert die Spielfigur, Kamera, Taschenlampe sowie Türen, Luke und Leiter.</summary>
    /// <remarks>Die Bewegung läuft auch beim Klettern über den CharacterController, damit Kollisionen erhalten bleiben.</remarks>
    [RequireComponent(typeof(CharacterController))]
    public class VillaExplorer : MonoBehaviour
    {
        // Im Inspector die Spielerkamera zuweisen. climbSpeed ist die Klettergeschwindigkeit in Metern pro Sekunde.
        public Camera view;
        public float climbSpeed = 1.5f;

        [Header("Springen")]
        [Tooltip("Maximale Sprunghöhe in Metern. Absichtlich niedrig gehalten.")]
        public float jumpHeight = 0.5f;

        /// <summary>Wird erst durch das Aufnehmen der Taschenlampe freigeschaltet; kein dauerhafter Spielstand.</summary>
        public bool HasFlashlight { get; private set; }

        [Header("Rätsel-Oberfläche")]
        public VillaPuzzleUI puzzleUI;

        [Header("Interaktions-Highlight")]
        public bool highlightInteractions = true;
        public Color interactionHighlightColor = new Color(1, .8f, .35f, 1);

        // Nur die Darstellung des anvisierten Objekts ändern, niemals sein gemeinsam genutztes Material.
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        readonly List<HighlightState> highlightedRenderers = new List<HighlightState>();
        Transform highlightedTarget;
        Color appliedHighlightColor;

        class HighlightState
        {
            public Renderer renderer;
            public int materialIndex;
            public MaterialPropertyBlock originalProperties;
        }

        // pitch: vertikaler Blickwinkel; fall: vertikale Geschwindigkeit; cooldown: kurze Sperre nach dem Verlassen eines Leiterendes.
        const float Gravity = 20f;
        float pitch, fall, cooldown;
        bool jumpQueued;
        CharacterController body;
        VillaLadder activeLadder;

        // Zwei Übergänge: oben zur Leiter ausrichten bzw. nach dem Aufstieg auf den Dachboden gehen.
        bool enteringFromTop, steppingOff;

        // Kurz eingeblendete Rückmeldung, wenn die Luke durch die Spielfigur blockiert ist.
        string message;
        float messageUntil;

        /// <summary>Gibt an, ob die Figur aktuell an einer Leiter hängt.</summary>
        public bool IsClimbing
        {
            get
            {
                return activeLadder != null;
            }
        }

        // CharacterController holen und die Maus für die Ego-Steuerung einfangen.
        void Start()
        {
            body = GetComponent<CharacterController>();
            HasFlashlight = false;
            var flashlight = view ? view.GetComponentInChildren<Light>(true) : null;
            if (flashlight)
                flashlight.enabled = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>Schaltet den Besitz frei. Mehrfaches Aufnehmen wird abgefangen.</summary>
        public bool CollectFlashlight()
        {
            if (HasFlashlight)
                return false;
            HasFlashlight = true;
            return true;
        }

        /// <summary>Schaltet ausschließlich die bereits aufgenommene Taschenlampe um.</summary>
        public bool ToggleFlashlight()
        {
            if (!HasFlashlight || !view)
                return false;
            var flashlight = view.GetComponentInChildren<Light>(true);
            if (!flashlight)
                return false;
            flashlight.enabled = !flashlight.enabled;
            return true;
        }


        // Eingaben des neuen Unity Input Systems lesen. Bei freigegebener Maus pausiert diese Steuerung.
        void Update()
        {
            if (puzzleUI && puzzleUI.BlocksPlayerInput)
                return;

            var k = Keyboard.current;
            var m = Mouse.current;
            if (k == null || m == null)
                return;
            if (k.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (m.rightButton.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
                return;

            // Horizontal dreht sich die Figur, vertikal nur die Kamera. Der Blick bleibt auch beim Klettern steuerbar.
            Vector2 look = m.delta.ReadValue() * .09f;
            transform.Rotate(0, look.x, 0);
            pitch = Mathf.Clamp(pitch - look.y, -85, 85);
            view.transform.localEulerAngles = new Vector3(pitch, 0, 0);

            // E bedient Türen, Luke und Kassettenrekorder unabhängig davon, ob die Figur gerade klettert.
            if (k.eKey.wasPressedThisFrame)
                Interact();

            // X = seitwärts, Y = vorwärts/rückwärts. Diagonale Bewegung wird in StepMovement begrenzt.
            var input = new Vector2((k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0),
                (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
            if (k.spaceKey.wasPressedThisFrame)
                jumpQueued = true;
            StepMovement(input, Time.deltaTime, k.leftShiftKey.isPressed ? 5 : 3);

            // F funktioniert erst, nachdem die Taschenlampe gefunden und aufgenommen wurde.
            if (k.fKey.wasPressedThisFrame)
                ToggleFlashlight();

            // Um Screenshots im Game-Modus aufzunehmen
            if (k.f12Key.wasPressedThisFrame)
            {
                string filename = $"Screenshot_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
                ScreenCapture.CaptureScreenshot(filename);
                Debug.Log($"Screenshot gespeichert: {filename}");
            }
        }

        // Nach Bewegung und Türanimation prüfen, damit die Hervorhebung zum aktuellen Blick passt.
        void LateUpdate()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                ClearInteractionHighlight();
                return;
            }

            UpdateInteractionHighlight();
        }

        /// <summary>Hebt das mit E erreichbare Blickziel hervor; Geheimtüren bleiben unmarkiert.</summary>
        void UpdateInteractionHighlight()
        {
            Transform target = null;
            float highlightStrength = .30f;
            VillaDoor door;
            VillaLadder hatch;
            VillaCassettePlayer cassette;
            VillaLightSwitch lightSwitch;
            VillaPuzzleObject puzzleObject;
            if (highlightInteractions && Target(out door, out hatch, out cassette, out lightSwitch, out puzzleObject))
            {
                if (door && !door.hideInteractionHint)
                    target = door.transform;
                else if (hatch)
                    target = hatch.hatchPivot; // Nur den beweglichen Deckel markieren, nicht die ganze Leiter.
                else if (cassette)
                    target = cassette.transform;
                else if (lightSwitch)
                    target = lightSwitch.transform;
                else if (puzzleObject)
                {
                    target = puzzleObject.transform;
                    highlightStrength = puzzleObject.highlightStrength;
                }
            }

            // Renderer und Ausgangswerte nur bei einem Ziel- oder Farbwechsel neu erfassen.
            if (target == highlightedTarget && appliedHighlightColor == interactionHighlightColor)
                return;

            ClearInteractionHighlight();
            if (!target)
                return;

            highlightedTarget = target;
            appliedHighlightColor = interactionHighlightColor;
            foreach (var renderer in target.GetComponentsInChildren<Renderer>())
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (!material || !material.HasProperty(BaseColorId))
                        continue;

                    // Vorhandene individuelle Renderer-Einstellungen nachher exakt wiederherstellen.
                    var original = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(original, i);
                    var highlighted = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(highlighted, i);
                    if (highlighted.isEmpty)
                        renderer.GetPropertyBlock(highlighted);

                    Color originalColor = highlighted.HasColor(BaseColorId)
                        ? highlighted.GetColor(BaseColorId) : material.GetColor(BaseColorId);
                    // Rätselobjekte können bei Bedarf deutlicher hervorgehoben werden.
                    Color tint = Color.Lerp(originalColor, interactionHighlightColor, highlightStrength);
                    tint.a = originalColor.a; // Transparenz des Materials erhalten.
                    highlighted.SetColor(BaseColorId, tint);
                    renderer.SetPropertyBlock(highlighted, i);
                    highlightedRenderers.Add(new HighlightState
                    {
                        renderer = renderer,
                        materialIndex = i,
                        originalProperties = original
                    });
                }
            }
        }

        /// <summary>Entfernt das Highlight bei Zielwechsel, Reichweitenverlust oder deaktivierter Steuerung.</summary>
        void ClearInteractionHighlight()
        {
            foreach (var state in highlightedRenderers)
                if (state.renderer)
                    state.renderer.SetPropertyBlock(state.originalProperties.isEmpty ? null : state.originalProperties,
                        state.materialIndex);

            highlightedRenderers.Clear();
            highlightedTarget = null;
        }

        /// <summary>Führt einen Bewegungsschritt mit Lauf- oder Leitersteuerung aus.</summary>
        /// <param name="input">Lokale Bewegungsachsen: X seitwärts, Y vorwärts/rückwärts.</param>
        /// <param name="dt">Vergangene Zeit in Sekunden; große Zeitschritte werden begrenzt.</param>
        /// <param name="walkSpeed">Gehgeschwindigkeit in Metern pro Sekunde.</param>
        public void StepMovement(Vector2 input, float dt, float walkSpeed = 3)
        {
            if (!body)
                body = GetComponent<CharacterController>();
            if (!body.enabled || !view)
                return;

            dt = Mathf.Clamp(dt, 0, .1f);
            cooldown = Mathf.Max(0, cooldown - dt);

            // Außerhalb der Leiter gelten die normalen, körperbezogenen Bewegungsachsen.
            Vector3 movement = Vector3.ClampMagnitude(
                transform.right * input.x + transform.forward * input.y, 1);

            // Zum Einstieg unten W, oben S drücken. Blick- und Körperausrichtung sind dafür egal.
            if (!activeLadder && cooldown <= 0 && Mathf.Abs(input.y) > .1f)
            {
                foreach (var ladder in VillaLadder.Active)
                {
                    bool fromTop;
                    if (ladder && ladder.CanGrab(transform.position, input.y, out fromTop))
                    {
                        activeLadder = ladder;
                        enteringFromTop = fromTop;
                        steppingOff = false;
                        fall = 0;
                        break;
                    }
                }
            }

            if (activeLadder)
            {
                // Die Figur bleibt bis zu einem Leiterende gebunden. A/D werden vollständig ignoriert.
                // Ohne W/S hält sie ihre Position; die Kamera darf sich weiterhin frei drehen.
                jumpQueued = false;
                fall = 0;
                if (Mathf.Abs(input.y) <= .1f)
                    return;

                var ladder = activeLadder;
                float bottom = ladder.lowerExit.position.y;
                float top = ladder.upperExit.position.y;
                float directionY = Mathf.Sign(input.y); // W positiv = hoch, S negativ = runter.

                Vector3 target = ladder.lowerClimb.position;
                target.y = transform.position.y;
                Vector3 correction = Vector3.ClampMagnitude(target - transform.position, 3 * dt);

                // Ein Richtungswechsel beim oberen Ausstieg führt zuerst zurück über die Leiter.
                // So sinkt die Figur nicht neben der Öffnung gegen die Dachbodenplatte.
                if (steppingOff && directionY < 0)
                {
                    steppingOff = false;
                    enteringFromTop = true;
                }

                // Oben erst über der Leiter ausrichten und danach abwärts klettern.
                if (enteringFromTop)
                {
                    body.Move(correction);
                    Vector3 remaining = target - transform.position;
                    remaining.y = 0;
                    if (remaining.magnitude < .06f)
                        enteringFromTop = false;
                    return;
                }

                if (steppingOff)
                {
                    // W bewegt entlang des festen Austrittswegs, unabhängig vom Kamerablick.
                    // Bis hinter den Rahmen bleibt die Schwerkraft aus, damit niemand zurückfällt.
                    Vector3 exit = ladder.upperExit.position;
                    exit.y = transform.position.y;
                    body.Move(Vector3.ClampMagnitude(exit - transform.position, 3 * dt));

                    Vector3 remaining = exit - transform.position;
                    remaining.y = 0;
                    if (remaining.magnitude <= .12f)
                    {
                        ReleaseLadder();
                        fall = -2;
                    }
                    return;
                }

                float nextY = Mathf.Clamp(
                    transform.position.y + directionY * climbSpeed * dt, bottom, top);

                // Kollisionen bleiben aktiv: Eine geschlossene Luke stoppt den Aufstieg weiterhin.
                body.Move(correction + Vector3.up * (nextY - transform.position.y));

                if (directionY > 0 && transform.position.y >= top - .035f)
                    steppingOff = true;

                // Nur am unteren Ende oder nach dem vollständigen oberen Ausstieg wieder freigeben.
                if (directionY < 0 && transform.position.y <= bottom + .04f)
                {
                    ReleaseLadder();
                    fall = -2;
                }
                return;
            }

            // Normales Gehen: Am Boden startet Leertaste einen niedrigen Sprung,
            // ansonsten sorgt die Schwerkraft (20 m/s²) für das Fallen.
            if (body.isGrounded)
            {
                if (jumpQueued)
                {
                    // v = sqrt(2 * g * h): h ist die im Inspector eingestellte Sprunghöhe.
                    fall = Mathf.Sqrt(2f * Gravity * Mathf.Max(0f, jumpHeight));
                    jumpQueued = false;
                }
                else
                {
                    fall = -2;
                }
            }
            else
            {
                fall -= Gravity * dt;
                jumpQueued = false;
            }

            body.Move((movement * walkSpeed + Vector3.up * fall) * dt);
        }

        // Die kurze Sperre verhindert ein sofortiges erneutes Greifen nach dem Ausstieg an einem Leiterende.
        void ReleaseLadder()
        {
            activeLadder = null;
            enteringFromTop = false;
            steppingOff = false;
            cooldown = .3f;
            fall = 0;
        }

        /// <summary>Sucht im Kamerablick bis zu zwei Meter weit nach Tür, Luke oder Kassettenrekorder.</summary>
        bool Target(out VillaDoor door, out VillaLadder hatch, out VillaCassettePlayer cassette, out VillaLightSwitch lightSwitch, out VillaPuzzleObject puzzleObject)
        {
            door = null;
            hatch = null;
            cassette = null;
            lightSwitch = null;
            puzzleObject = null;
            if (!view)
                return false;

            // Ein gemeinsamer Blickstrahl für alle Interaktionen; Wände blockieren die Auswahl.
            RaycastHit hit;
            if (!Physics.Raycast(view.transform.position, view.transform.forward, out hit, 2))
                return false;

            puzzleObject = hit.collider.GetComponentInParent<VillaPuzzleObject>();
            if (puzzleObject && puzzleObject.isActiveAndEnabled)
                return true;
            puzzleObject = null;

            door = hit.collider.GetComponentInParent<VillaDoor>();
            if (door)
                return true;

            cassette = hit.collider.GetComponentInParent<VillaCassettePlayer>();
            if (cassette && cassette.isActiveAndEnabled)
                return true;
            cassette = null;

            lightSwitch = hit.collider.GetComponentInParent<VillaLightSwitch>();
            if (lightSwitch && lightSwitch.isActiveAndEnabled)
                return true;
            lightSwitch = null;

            // Von unten darf der Blickstrahl die Leiter treffen: Bei Blick nach oben und naher Luke wird diese bedienbar.
            var ladder = hit.collider.GetComponentInParent<VillaLadder>();
            if (ladder
                && (ladder.IsHatch(hit.collider)
                || (view.transform.forward.y > .25f
                && Vector3.Distance(view.transform.position, ladder.ClosedHatchCenter) < 2)))
            {
                hatch = ladder;
                return true;
            }

            return false;
        }

        /// <summary>Bedient das anvisierte Ziel. Liefert false, wenn kein Ziel gefunden oder die Luke blockiert wird.</summary>
        public bool Interact()
        {
            if (puzzleUI && puzzleUI.IsOpen)
                return false;

            VillaDoor door;
            VillaLadder hatch;
            VillaCassettePlayer cassette;
            VillaLightSwitch lightSwitch;
            VillaPuzzleObject puzzleObject;
            if (!Target(out door, out hatch, out cassette, out lightSwitch, out puzzleObject))
                return false;
            if (puzzleObject)
                return puzzleObject.TryInteract();

            if (door)
            {
                door.Toggle(transform.position);
                return true;
            }

            if (cassette)
            {
                if (cassette.TryToggle())
                    return true;

                message = cassette.UnavailableMessage;
                messageUntil = Time.unscaledTime + 2.5f;
                return false;
            }

            if (lightSwitch)
                return lightSwitch.TryToggle();

            if (hatch.TryToggleHatch())
                return true;
            message = "Luke blockiert – erst den Durchstieg freimachen";
            messageUntil = Time.unscaledTime + 1.5f;
            return false;
        }

        // Auch beim Abschalten der Komponente oder Beenden des Spielmodus die Maus wieder freigeben.
        void OnDisable()
        {
            ClearInteractionHighlight();
            ReleaseLadder();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Einfache Prototyp-Anzeige: Steuerung, Fadenkreuz und Hinweise passend zum anvisierten Objekt.
        void OnGUI()
        {
            if (puzzleUI && puzzleUI.IsOpen)
                return;

            GUI.color = new Color(1, 1, 1, .8f);
            GUI.Label(new Rect(Screen.width / 2 - 4, Screen.height / 2 - 10, 15, 25), "+");
            string hint = null;
            VillaDoor door;
            VillaLadder hatch;
            VillaCassettePlayer cassette;
            VillaLightSwitch lightSwitch;
            VillaPuzzleObject puzzleObject;

            // Hinweis erscheint nicht bei Geheimtür
            if (Target(out door, out hatch, out cassette, out lightSwitch, out puzzleObject) && (!door || !door.hideInteractionHint))
            {
                if (door)
                    hint = "E · " + door.interactionName + (door.open ? " schließen" : " öffnen");
                else if (hatch)
                    hint = hatch.IsOpen ? "E · Luke schließen" : "E · Luke öffnen";
                else if (cassette)
                    hint = cassette.InteractionHint;
                else if (lightSwitch)
                    hint = lightSwitch.IsOn ? "E · Licht ausschalten" : "E · Licht einschalten";
                else if (puzzleObject)
                    hint = puzzleObject.InteractionHint;
            }

            // Die kurzzeitige Fehlermeldung hat Vorrang vor dem normalen Interaktionshinweis.
            if (Time.unscaledTime < messageUntil)
                hint = message;

            var centeredStyle = new GUIStyle(GUI.skin.label);
            centeredStyle.alignment = TextAnchor.MiddleCenter;

            if (hint != null)
                GUI.Label(new Rect(Screen.width / 2 - 250, Screen.height - 80, 500, 25), hint, centeredStyle);
            if (activeLadder)
                GUI.Label(new Rect(Screen.width / 2 - 300, Screen.height - 50,
                    600, 25), "Leiter: W hoch · S runter", centeredStyle);
        }
    }
}
