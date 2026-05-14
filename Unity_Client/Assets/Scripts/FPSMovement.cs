using UnityEngine;

public class FPSMovement : MonoBehaviour
{
    [Header("Referanslar")]
    public CharacterController controller;
    public Transform playerCamera;

    [Header("Hareket Ayarları")]
    public float speed = 5f;
    public float mouseSensitivity = 200f;
    public float gravity = -19.62f; 
    public float jumpHeight = 1.8f;

    [Header("Ground Check")]
    public float groundCheckDistance = 0.35f;
    public LayerMask groundMask = ~0; 

    // Dahili değişkenler
    private float xRotation = 0f;
    private Vector3 velocity;
    private bool isGrounded;
    private Vector3 currentMoveVelocity;
    private Vector3 moveDampVelocity;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (Cursor.lockState == CursorLockMode.None) return;
        GroundCheck();
        HandleMouseLook();
        HandleMovement();
        HandleJump();
        ApplyGravity();
    }

    void GroundCheck()
    {
        // CharacterController.isGrounded + SphereCast ile çift kontrol ediyor
        
        isGrounded = controller.isGrounded;

        if (!isGrounded)
        {
            // Ek kontrol: controller.isGrounded bazen false döner ama aslında yerdeyizdir
            float sphereRadius = controller.radius * 0.9f;
            float checkStart = controller.height / 2f - sphereRadius;
            isGrounded = Physics.SphereCast(
                transform.position + controller.center,
                sphereRadius,
                Vector3.down,
                out _,
                checkStart + groundCheckDistance,
                groundMask,
                QueryTriggerInteraction.Ignore
            );
        }

        // Yerdeyken düşme hızını sıfırla (ama hafif aşağı çek ki yere yapışsın)
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -85f, 85f);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 targetVelocity = (transform.right * x + transform.forward * z) * speed;

        // Smooth hareket geçişi — ani duruş/kalkış yok
        currentMoveVelocity = Vector3.SmoothDamp(
            currentMoveVelocity,
            targetVelocity,
            ref moveDampVelocity,
            isGrounded ? 0.1f : 0.2f // Havadayken daha yavaş tepki
        );

        controller.Move(currentMoveVelocity * Time.deltaTime);
    }

    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}