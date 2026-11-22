using UnityEngine;

public class KillerFootsteps : MonoBehaviour
{
    public AudioClip[] schrittSounds;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Funktion wird von der Animation aufgerufen, um Schrittgeräusche synchron mit Model zu halten
    public void PlayFootstep()
    {
        if (schrittSounds.Length == 0 || audioSource == null) return;

        // Zufällige Fußschritt-Geräusche für mehr Abwechslung
        int zufallsIndex = Random.Range(0, schrittSounds.Length);
        
        // Anhebung der Tonhöhe für natürlicheren Klang
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        
        audioSource.PlayOneShot(schrittSounds[zufallsIndex]);
    }
}
