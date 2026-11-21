using UnityEngine;
using UnityEngine.InputSystem;

namespace FamiliarDread
{
    /// <summary>Einfache Lese- und PC-Oberfläche ohne Betriebssystemsimulation oder Dateibrowser.</summary>
    public class VillaPuzzleUI : MonoBehaviour
    {
        enum Page { None, Document, Password, Inbox, Mail }
        public VillaPuzzleState story;

        Page page;
        VillaPuzzleObject current;
        VillaCassettePlayer transcriptSource;
        string passwordInput = "", error = "", toast;
        float toastUntil;
        Vector2 scroll;
        bool focusPassword;
        int closedFrame = -1;
        CursorLockMode previousLock;
        bool previousVisible;
        GUIStyle heading, body, button, errorStyle, toastStyle;

        public bool IsOpen { get { return page != Page.None; } }
        // Verhindert, dass dasselbe Escape im Spieler-Update die Maus sofort wieder freigibt.
        public bool BlocksPlayerInput { get { return IsOpen || Time.frameCount == closedFrame; } }
        public string CurrentPage { get { return page.ToString(); } }

        void Begin(VillaPuzzleObject source, Page next)
        {
            if (IsOpen)
                Close();
            previousLock = Cursor.lockState;
            previousVisible = Cursor.visible;
            current = source;
            page = next;
            scroll = Vector2.zero;
            error = "";
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void OpenDocument(VillaPuzzleObject source) { Begin(source, Page.Document); }

        public void OpenComputer(VillaPuzzleObject source)
        {
            Begin(source, story.PcUnlocked ? Page.Inbox : Page.Password);
            source.SetComputerPowered(true);
            passwordInput = "";
            focusPassword = true;
        }

        public bool TryLogin(string input)
        {
            if (page != Page.Password)
                return false;
            if (!story.TryUnlock(input))
            {
                error = "Passwort falsch. Prüfe Ort und Jahr auf dem Familienfoto.";
                return false;
            }
            page = Page.Inbox;
            passwordInput = "";
            error = "";
            return true;
        }

        public bool ReadMail()
        {
            if (page != Page.Inbox || !story.ReadMail())
                return false;
            page = Page.Mail;
            scroll = Vector2.zero;
            return true;
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            if (current && current.kind == VillaPuzzleObject.Kind.Computer)
                current.SetComputerPowered(false);
            current = null;
            page = Page.None;
            passwordInput = "";
            error = "";
            closedFrame = Time.frameCount;
            Cursor.lockState = previousLock;
            Cursor.visible = previousVisible;
        }

        public void ShowToast(string text)
        {
            toast = text;
            toastUntil = Time.unscaledTime + 4;
        }

        public void ShowTranscript(VillaCassettePlayer source) { transcriptSource = source; }

        void Update()
        {
            if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                Close();
        }

        void OnDisable() { Close(); }

        void EnsureStyles()
        {
            // Native GUIStyles können nach einem Editor-Reload ungültig sein.
            if (heading != null && heading.fontSize == 23 && body != null && body.fontSize == 17
                && toastStyle != null && toastStyle.fontSize == 17)
                return;
            heading = new GUIStyle(GUI.skin.label) { fontSize = 23, fontStyle = FontStyle.Bold, wordWrap = true };
            heading.normal.textColor = Color.white;
            body = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true, richText = false };
            body.normal.textColor = new Color(.88f, .89f, .85f);
            // Nur die Meldungsleiste zentrieren; Dokumente und Transkript behalten ihre Ausrichtung.
            toastStyle = new GUIStyle(body) { alignment = TextAnchor.MiddleCenter };
            button = new GUIStyle(GUI.skin.button) { fontSize = 17, fixedHeight = 38 };
            button.normal.textColor = Color.white;
            button.hover.textColor = Color.white;
            button.active.textColor = Color.white;
            errorStyle = new GUIStyle(body);
            errorStyle.normal.textColor = new Color(1, .52f, .43f);
        }

        void DrawPanel(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        void OnGUI()
        {
            EnsureStyles();
            Color oldColor = GUI.color;
            try
            {
                GUI.color = Color.white;
                if (!IsOpen)
                {
                    // Das Transkript blockiert die Bewegung und die E-Stoppfunktion des Rekorders nicht.
                    if (transcriptSource && transcriptSource.IsPlaying && story.TapeCollected)
                    {
                        float w = Mathf.Min(420, Screen.width - 32);
                        Rect area = new Rect(Screen.width - w - 16, 16, w, Mathf.Min(350, Screen.height - 120));
                        DrawPanel(area, new Color(.035f, .045f, .045f, .95f));
                        GUILayout.BeginArea(new Rect(area.x + 16, area.y + 12, area.width - 32, area.height - 24));
                        scroll = GUILayout.BeginScrollView(scroll);
                        GUILayout.Label(story.tapeTranscript, body);
                        GUILayout.EndScrollView();
                        GUILayout.EndArea();
                    }
                    if (Time.unscaledTime < toastUntil)
                    {
                        Rect area = new Rect(24, Screen.height - 155, Screen.width - 48, 65);
                        DrawPanel(area, new Color(.035f, .045f, .045f, .93f));
                        GUI.Label(new Rect(area.x + 16, area.y + 10, area.width - 32, area.height - 20), toast, toastStyle);
                    }
                    return;
                }

                DrawPanel(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0, .65f));
                float width = Mathf.Min(720, Screen.width - 32);
                float height = Mathf.Min(560, Screen.height - 32);
                Rect panel = new Rect((Screen.width - width) / 2, (Screen.height - height) / 2, width, height);
                DrawPanel(panel, new Color(.055f, .07f, .075f, .99f));
                GUILayout.BeginArea(new Rect(panel.x + 24, panel.y + 20, panel.width - 48, panel.height - 40));
                bool pc = current && current.kind == VillaPuzzleObject.Kind.Computer;
                GUILayout.Label(pc ? "FAMILIEN-PC" : current ? current.title : "Hinweis", heading);
                GUILayout.Space(12);
                scroll = GUILayout.BeginScrollView(scroll);

                if (page == Page.Document && current)
                {
                    if (current.kind == VillaPuzzleObject.Kind.Photo)
                    {
                        if (story.photo)
                            GUILayout.Label(story.photo, GUILayout.Width(Mathf.Min(440, width - 70)), GUILayout.Height(Mathf.Min(240, height * .43f)));
                        GUILayout.Space(12);
                        GUILayout.Label(story.photoCaption, body);
                    }
                    else
                        GUILayout.Label(current.DocumentText, body);
                }
                else if (page == Page.Password)
                {
                    GUILayout.Label("Benutzer: Familie", body);
                    GUILayout.Space(16);
                    GUILayout.Label("Passwort", body);
                    GUI.SetNextControlName("PuzzlePassword");
                    passwordInput = GUILayout.PasswordField(passwordInput, '*', 64, GUILayout.Height(34));
                    GUILayout.Label(story.passwordFormat, body);
                    GUILayout.Space(12);
                    if (GUILayout.Button("Anmelden", button))
                        TryLogin(passwordInput);
                    if (!string.IsNullOrEmpty(error))
                        GUILayout.Label(error, errorStyle);
                    if (focusPassword && Event.current.type == EventType.Repaint)
                    {
                        GUI.FocusControl("PuzzlePassword");
                        focusPassword = false;
                    }
                    if (Event.current.type == EventType.KeyDown &&
                        (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
                    {
                        TryLogin(passwordInput);
                        Event.current.Use();
                    }
                }
                else if (page == Page.Inbox)
                {
                    GUILayout.Label("E-Mails", heading);
                    GUILayout.Space(12);
                    if (GUILayout.Button(story.mailSender + " · " + story.mailSubject, button))
                        ReadMail();
                }
                else if (page == Page.Mail)
                {
                    GUILayout.Label(story.mailSubject, heading);
                    GUILayout.Label("Von: " + story.mailSender, body);
                    GUILayout.Space(12);
                    GUILayout.Label(story.mailBody, body);
                }
                GUILayout.EndScrollView();
                GUILayout.Space(12);
                if (page == Page.Mail && GUILayout.Button("Zurück zu E-Mails", button))
                    page = Page.Inbox;
                if (GUILayout.Button(pc ? "PC ausschalten (Esc)" : "Schließen (Esc)", button))
                    Close();
                GUILayout.EndArea();
            }
            finally { GUI.color = oldColor; }
        }
    }
}
