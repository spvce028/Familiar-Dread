using UnityEngine;
using UnityEngine.AI;

public class KillerPatrol : MonoBehaviour
{
    public Transform[] wegpunkte;
    public float warteZeit = 3f;

    private NavMeshAgent agent;
    private Animator animator;
    private int aktuellerPunkt = 0;
    private float timer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (wegpunkte.Length > 0)
        {
            agent.SetDestination(wegpunkte[0].position);
        }
    }

    void Update()
    {
        // Schaltet zwischen Idle und Walking Animation um
        // Schaut zuerst, ob bool lauftGerade true ist (läuft Killer?)
        // Setzt dann den Unity-Bool "isWalking" true bzw false und schaltet in Unity die Animationen um
        bool laeuftGerade = agent.velocity.magnitude > 0.1f;
        animator.SetBool("isWalking", laeuftGerade);

        if (wegpunkte.Length == 0) return;

        // Prüft, ob Jess den aktuellen Wegpunkt erreicht hat
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            timer += Time.deltaTime;

            // Wartet kurz im Idle Zustand, bevor sie zum nächsten Punkt geht
            if (timer >= warteZeit)
            {
                aktuellerPunkt = (aktuellerPunkt + 1) % wegpunkte.Length;
                agent.SetDestination(wegpunkte[aktuellerPunkt].position);
                timer = 0f;
            }
        }
    }
}
