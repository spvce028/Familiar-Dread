using UnityEngine;

public class FlickeringLight : MonoBehaviour
{
    [Header("Light")]
    [SerializeField] private Light targetLight;

    [Header("Flicker")]
    [SerializeField] private float minFlickerTime = 0.05f;
    [SerializeField] private float maxFlickerTime = 0.2f;

    private float timer;
    private bool isFlickering;

    private void Start()
    {
        SetRandomTimer();
    }

    private void Update()
    {

        if (!isFlickering)
            return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            targetLight.enabled = !targetLight.enabled;
            SetRandomTimer();
        }
    }

    public void StartFlickering()
    {
        isFlickering = true;
        SetRandomTimer();
    }

    public void StopFlickering()
    {
        isFlickering = false;
        targetLight.enabled = true;
    }

    private void SetRandomTimer()
    {
        timer = Random.Range(minFlickerTime, maxFlickerTime);
    }
}
