using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;

    [Header("Mouse")]
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private Transform playerCamera;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;

    private CharacterController controller;

    private float verticalVelocity;
    private float cameraRotation;

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleMovement();
        HandleMouseLook();
    }

    private void HandleMovement()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            input.x =
                (Keyboard.current.dKey.isPressed ? 1f : 0f) -
                (Keyboard.current.aKey.isPressed ? 1f : 0f);

            input.y =
                (Keyboard.current.wKey.isPressed ? 1f : 0f) -
                (Keyboard.current.sKey.isPressed ? 1f : 0f);
        }

        Vector3 movement =
            transform.right * input.x +
            transform.forward * input.y;

        movement = Vector3.ClampMagnitude(movement, 1f);

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;
        movement.y = verticalVelocity;

        controller.Move(movement * moveSpeed * Time.deltaTime);
    }

    private void HandleMouseLook()
    {
        if (Mouse.current == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;

        cameraRotation -= mouseY;
        cameraRotation = Mathf.Clamp(cameraRotation, -85f, 85f);

        playerCamera.localRotation =
            Quaternion.Euler(cameraRotation, 0f, 0f);

        transform.Rotate(Vector3.up * mouseX);
    }
}
