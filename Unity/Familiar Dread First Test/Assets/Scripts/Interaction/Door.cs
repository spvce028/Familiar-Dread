using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    [Header("Door")]
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 3f;

    [Header("Lock")]
    [SerializeField] private bool isLocked = false;
    [SerializeField] private string requiredItemID;

    private bool isOpen;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    private void Start()
    {
        closedRotation = transform.localRotation;

        openRotation =
            closedRotation * Quaternion.Euler(0f, openAngle, 0f);
    }

    private void Update()
    {
        Quaternion targetRotation =
            isOpen ? openRotation : closedRotation;

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            targetRotation,
            openSpeed * Time.deltaTime
        );
    }

    public void Interact()
    {
        if (isLocked)
        {
            PlayerInventory inventory =
                FindFirstObjectByType<PlayerInventory>();

            if (inventory == null)
            {
                Debug.LogWarning("Kein PlayerInventory gefunden.");
                return;
            }

            if (!inventory.HasItem(requiredItemID))
            {
                Debug.Log("Die Tür ist verschlossen.");
                return;
            }

            isLocked = false;

            Debug.Log("Tür aufgeschlossen mit: " + requiredItemID);
        }

        isOpen = !isOpen;
    }
}
