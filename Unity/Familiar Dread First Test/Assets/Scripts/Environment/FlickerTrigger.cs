using UnityEngine;

public class FlickerTrigger : MonoBehaviour
{
    [SerializeField] private FlickeringLight flickeringLight;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            flickeringLight.StartFlickering();
        }
    }
}
