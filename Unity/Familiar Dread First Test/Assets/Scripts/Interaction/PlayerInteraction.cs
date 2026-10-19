using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionDistance = 2.5f;

    private void Update()
    {
        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryInteract();
        }

        //Raycast sichtbar machen 
        Debug.DrawRay(
            playerCamera.transform.position,
            playerCamera.transform.forward * interactionDistance,
            Color.red
        );
    }

    private void TryInteract()
    {
        //Raycast erstellen
        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        //Raycast durchführen
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            //Prüfen ob Objekt das mit Raycast gefunden wurde IInteractable implementiert
            IInteractable interactable =
                hit.collider.GetComponentInParent<IInteractable>();

            //Falls ja, Interact()-Methode ausführen
            if (interactable != null)
            {
                interactable.Interact();
            }
        }
    }
}