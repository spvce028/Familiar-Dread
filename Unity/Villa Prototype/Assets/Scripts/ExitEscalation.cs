using UnityEngine;
using FamiliarDread;

public class ExitEscalation : MonoBehaviour
{
    [Header("Verknüpfungen")]
    public VillaDoor ausgangsTuer;           // Ausgangstür
    public KillerKeyInventory spielerInventar; // Player_Kind
    public VillaPuzzleState story;             // Raetsel_2OG (für "RENN!")
    public AudioSource panikAudio;             // Dramatischer Sound
    public GameObject gameOverScreen;          // UI-Panel für Game Over
    public float panikTempo = 4.6f;            // Geschwindigkeit im Panik-Modus

    private KillerPatrol patrolSkript;
    private bool eskalationAktiv = false;

    void Start()
    {
        patrolSkript = GetComponent<KillerPatrol>();
        
        // Stellt sicher, dass der Game-Over-Bildschirm beim Start unsichtbar ist
        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(false);
        }
    }

    void Update()
    {
        // Prüft: Ausgangstür ist offen UND Spieler besitzt den Schlüssel noch
        if (!eskalationAktiv && ausgangsTuer != null && ausgangsTuer.open && spielerInventar != null && spielerInventar.hatSchluessel)
        {
            StartePanikModus();
        }

        // Kontakt mit dem Spieler im Panik-Modus -> Game Over
        if (eskalationAktiv && spielerInventar != null)
        {
            float abstand = Vector3.Distance(transform.position, spielerInventar.transform.position);
            if (abstand <= 1.15f)
            {
                LoeseGameOverAus();
            }
        }
    }

    void StartePanikModus()
    {
        eskalationAktiv = true;
        patrolSkript.istAggressiv = true;
        patrolSkript.verfolgungsTempo = panikTempo;
        // Hält keinen Abstand mehr
        patrolSkript.verfolgungsAbstand = 0.5f; 

        if (panikAudio != null) panikAudio.Play();
        if (story != null && story.ui != null)
        {
            story.ui.ShowToast("RENN!");
        }
    }

    void LoeseGameOverAus()
    {
        if (gameOverScreen != null) gameOverScreen.SetActive(true);
        // Friert das Spiel ein
        Time.timeScale = 0f; 
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
