using UnityEngine;

public class LightSwitch : MonoBehaviour, IInteractable
{
    [Header("Light")]
    [SerializeField] private Light targetLight;

    public void Interact()
    {
        targetLight.enabled = !targetLight.enabled;
    }
}
