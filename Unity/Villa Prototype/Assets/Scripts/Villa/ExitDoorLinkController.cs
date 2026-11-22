using UnityEngine;
using Unity.AI.Navigation; // Wichtig für NavMeshLink
using FamiliarDread;         // Greift auf das Skript deines Teams zu


//Der Killer kommt nicht aus dem haus, deswegen wurde eine "Brücke" zwischen Außen- und Innenbereich platziert.
//Diese Brücke ist immer aktiv. Deswegen hilft dieses Skript, die Brücke nur dann zu aktivieren, wenn die Ausgangstür geöffnet wird.
public class ExitDoorLinkController : MonoBehaviour
{
    public NavMeshLink exitLink;
    private VillaDoor tuerSkript;

    void Start()
    {
        tuerSkript = GetComponent<VillaDoor>();
    }

    void Update()
    {
        if (tuerSkript != null && exitLink != null)
        {
            // Schaltet den Link synchron zum Öffnungszustand der Ausgangstür ein und aus
            exitLink.activated = tuerSkript.open;
        }
    }
}
