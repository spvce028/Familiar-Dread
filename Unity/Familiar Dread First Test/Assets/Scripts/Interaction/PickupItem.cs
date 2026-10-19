using UnityEngine;

public class PickupItem : MonoBehaviour, IInteractable
{
    [Header("Item")]
    [SerializeField] private string itemID;

    public void Interact()
    {
        //Skaliert schlecht, da ganze Szene nach inventory durchsucht wird
        //Funktioniert außerdem nicht falls es mal mehr als 1 inventory gibt
        PlayerInventory inventory =
            FindFirstObjectByType<PlayerInventory>();

        if (inventory == null)
        {
            Debug.LogWarning("Kein PlayerInventory gefunden.");
            return;
        }

        inventory.AddItem(itemID);

        Destroy(gameObject);
    }
}
