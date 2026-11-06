using UnityEngine;

public class LightSwitch : MonoBehaviour, IInteractable
{
    [Header("Light")]
    [SerializeField] private Light targetLight;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip switchSound;

    public void Interact()
    {
        targetLight.enabled = !targetLight.enabled;

        audioSource.PlayOneShot(switchSound);
    }
}
