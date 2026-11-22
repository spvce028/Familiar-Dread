using UnityEngine;
using UnityEngine.AI;

public class KillerPatrol : MonoBehaviour
{

    public bool istAggressiv = false;


    [Header("Patrouille")]
    public Transform[] wegpunkte;
    public float warteZeit = 4f;
    public float patrouillenTempo = 1.4f;


    [Header("Sichtlinie & Verfolgung")]
    public Transform spieler;
    public float verfolgungsAbstand = 1.6f; // Abstand, bei dem sie vor dem Spieler anhält
    public float sichtReichweite = 12f;
    [Range(0, 360)] public float sichtWinkel = 110f;
    public float verfolgungsTempo = 3.2f;
    public float zeitBisAufgabe = 4f; // Sekunden ohne Sichtkontakt, bis sie aufgibt
    public LayerMask hindernisLayer; // Hindernis-Layer für Wände und Türen


    [Header("Status (Für noch zu implementierende Mechaniken)")]
    public bool siehtSpielerGerade = false;
    public bool verfolgtSpieler = false;


    private NavMeshAgent agent;
    private Animator animator;
    private int aktuellerPunkt = 0;
    private float warteTimer = 0f;
    private float verlustTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        agent.speed = patrouillenTempo;



        if (spieler == null && GameObject.FindGameObjectWithTag("Player") != null)
        {
            spieler = GameObject.FindGameObjectWithTag("Player").transform;
        }

        GeheZuZufaelligemPunkt();
    }


    void Update()
    {
        // Animation synchron zur Bewegung steuern
        bool laeuftGerade = agent.velocity.magnitude > 0.1f;
        animator.SetBool("isWalking", laeuftGerade);

        // Damit der Killer sich nicht durch den Link zwischen Außen- und Innenbereich teleportiert, wenn sie aus der Tür geht.
        if (agent.isOnOffMeshLink)
        {
            OffMeshLinkData daten = agent.currentOffMeshLinkData;
            Vector3 zielPos = daten.endPos + Vector3.up * agent.baseOffset;

            transform.position = Vector3.MoveTowards(transform.position, zielPos, agent.speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, zielPos) < 0.05f)
            {
                agent.CompleteOffMeshLink();
            }
            return;
        }

        PruefeSichtlinie();

        if (verfolgtSpieler || istAggressiv)
        {
            VerfolgeSpieler();
        }
        else
        {
            Patrouilliere();
        }
    }

    void PruefeSichtlinie()
    {
        if (spieler == null) return;

        siehtSpielerGerade = false;

        // 1. Distanz prüfen (auf unsere Augenhöhe gemessen)
        Vector3 augenPosition = transform.position + Vector3.up * 1.6f;
        Vector3 spielerZiel = spieler.position + Vector3.up * 1.2f;
        Vector3 richtungZumSpieler = spielerZiel - augenPosition;
        float distanz = richtungZumSpieler.magnitude;

        if (distanz <= sichtReichweite)
        {
            // 2. Blickwinkel prüfen
            float winkel = Vector3.Angle(transform.forward, richtungZumSpieler.normalized);
            if (winkel <= sichtWinkel * 0.5f)
            {
                // 3. Raycast prüfen (Ist eine Wand oder Tür dazwischen?)
                if (!Physics.Raycast(augenPosition, richtungZumSpieler.normalized, distanz, hindernisLayer))
                {
                    siehtSpielerGerade = true;
                    verfolgtSpieler = true;
                    verlustTimer = 0f;
                }
            }
        }
    }

    void VerfolgeSpieler()
    {
        agent.speed = verfolgungsTempo;
        agent.stoppingDistance = verfolgungsAbstand;
        agent.SetDestination(spieler.position);

        // Dreht sich im Stehen weiter zum Spieler um, falls sie direkt vor ihm steht
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            Vector3 blickRichtung = (spieler.position - transform.position).normalized;
            blickRichtung.y = 0; // Verhindert, dass sie sich nach oben/unten neigt
            if (blickRichtung != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(blickRichtung), Time.deltaTime * 5f);
            }
        }

        // Wenn kein Sichtkontakt mehr besteht, Timer hochzählen
        if (!siehtSpielerGerade && !istAggressiv)
        {
            verlustTimer += Time.deltaTime;
            if (verlustTimer >= zeitBisAufgabe)
            {
                // Verfolgung abbrechen und zum Startpunkt zurückkehren
                verfolgtSpieler = false;
                agent.speed = patrouillenTempo;
                agent.stoppingDistance = 0.2f;
                aktuellerPunkt = 0;
                if (wegpunkte.Length > 0)
                {
                    agent.SetDestination(wegpunkte[0].position);
                }
            }
        }
    }

    void Patrouilliere()
    {
        if (wegpunkte.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            warteTimer += Time.deltaTime;
            if (warteTimer >= warteZeit)
            {
                GeheZuZufaelligemPunkt();
                warteTimer = 0f;
            }
        }
    }

    void GeheZuZufaelligemPunkt(){

        if (wegpunkte.Length == 0) return;
        
        if (wegpunkte.Length > 1){

            int naechster = aktuellerPunkt;
            while (naechster == aktuellerPunkt){
                naechster = Random.Range(0, wegpunkte.Length);
            }
            aktuellerPunkt = naechster;
        }
        agent.SetDestination(wegpunkte[aktuellerPunkt].position);
        }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = siehtSpielerGerade ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.6f, sichtReichweite);

        Vector3 links = Quaternion.Euler(0, -sichtWinkel * 0.5f, 0) * transform.forward;
        Vector3 rechts = Quaternion.Euler(0, sichtWinkel * 0.5f, 0) * transform.forward;

        Gizmos.DrawLine(transform.position + Vector3.up * 1.6f, transform.position + Vector3.up * 1.6f + links * sichtReichweite);
        Gizmos.DrawLine(transform.position + Vector3.up * 1.6f, transform.position + Vector3.up * 1.6f + rechts * sichtReichweite);
    }

}
