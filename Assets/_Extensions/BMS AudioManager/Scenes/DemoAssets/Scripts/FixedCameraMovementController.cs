using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixedCameraMovementController : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float rotationSpeed = 720f;
    private Transform cameraTransform; // Reference to the fixed camera transform

    private Rigidbody myRigidbody;
    private Vector3 movement;

    void Awake()
    {
        myRigidbody = GetComponent<Rigidbody>();
        
        //get the camera transform from the main camera
        cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        // Get movement input relative to the camera's forward and right direction
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        // Project movement on the XZ plane (ignore Y)
        forward.y = 0f;
        right.y = 0f;

        // Normalise directions
        forward.Normalize();
        right.Normalize();

        float horizontal = 0f;
        float vertical = 0f;
#if ENABLE_LEGACY_INPUT_MANAGER
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");
#elif ENABLE_INPUT_SYSTEM
        // New Input System only (Unity 6 default) - read WASD / arrow keys directly
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null)
        {
            horizontal = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                       - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            vertical   = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                       - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
        }
#endif

        // Calculate desired movement direction
        movement = (forward * vertical + right * horizontal).normalized * moveSpeed;

        // Rotate the player to face movement direction, if any input is detected
        if (movement.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        // Apply movement
        myRigidbody.MovePosition(myRigidbody.position + movement * Time.fixedDeltaTime);
    }
}
