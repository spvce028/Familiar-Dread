using UnityEngine;
using Unity.AI.Navigation;
using FamiliarDread;


//Der Killer kommt nicht aus dem haus, deswegen wurde eine "Brücke" zwischen Außen- und Innenbereich platziert.
//Diese Brücke ist immer aktiv. Deswegen hilft dieses Skript, die Brücke nur dann zu aktivieren, wenn die Ausgangstür geöffnet wird.
public class ExitDoorLinkController : MonoBehaviour
{
    public NavMeshLink exitLink;
    // Prüft, ob der Spieler den Schlüssel hat
    public KillerKeyInventory spielerInventar; 
    // Zeigt die Meldung "Verschlossen..." an
    public VillaPuzzleState story;             

    private VillaDoor tuerSkript;
    private float meldungCooldown = 0f;

    void Start()
    {
        tuerSkript = GetComponent<VillaDoor>();
    }

    void Update(){
        if (tuerSkript == null) return;

        if (meldungCooldown > 0f)
        {
            meldungCooldown -= Time.deltaTime;
        }

        // Versucht der Spieler die Tür zu öffnen, obwohl er keinen Schlüssel hat?
        if (tuerSkript.open && spielerInventar != null && !spielerInventar.hatSchluessel)
        {
            // Hält die Ausgangstür sofort verschlossen
            tuerSkript.open = false;

            // Zeigt dem Spieler "Verschlossen" mit Cooldown, damit es nicht spammt
            if (meldungCooldown <= 0f && story != null && story.ui != null)
            {
                story.ui.ShowToast("Verschlossen.");
                meldungCooldown = 2f;
            }
        }

        // Schaltet den NavMesh-Link nur frei, wenn die Tür wirklich mit Schlüssel geöffnet wurde
        if (exitLink != null)
        {
            exitLink.activated = tuerSkript.open;
        }
    }
}
