using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    private CharacterController controller;
    private Animator animator;

    private float speed = 5f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.2f;
    private Vector3 velocity;
    public float mouseSensitivity = 100f;

    [Header("Çömelme Ayarlarý")]
    public float originalHeight = 2f;
    public float duckHeight = 1f;
    private Vector3 originalCenter;
    private Vector3 duckCenter;
  
    private void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        if (controller != null)
        {
            originalHeight = controller.height;
            originalCenter = controller.center;
            duckCenter = new Vector3(originalCenter.x, originalCenter.y / 2f, originalCenter.z);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (!IsOwner) return;

        MovePlayer();
        HandleShooting();
      
    }

    private void MovePlayer()
    {
        bool isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        
        bool isDucking = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C);
       
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;

        if (isDucking)
        {          
            move = Vector3.zero;
            
            controller.height = duckHeight;
            controller.center = duckCenter;
        }
        else
        {            
            controller.height = originalHeight;
            controller.center = originalCenter;
        }
       
        controller.Move(move * speed * Time.deltaTime);

        // --- Zýplama ---
        if (Input.GetButtonDown("Jump") && isGrounded && !isDucking)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // --- Yerçekimi ---
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // --- Fare Dönüþü ---
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);

        // --- ANÝMATÖR BÝLGÝ GÖNDERÝMÝ ---
        if (animator != null)
        {
            animator.SetFloat("Speed", move.magnitude);
            animator.SetBool("isGrounded", isGrounded);
            animator.SetBool("isDucking", isDucking);
        }
    }

    private void HandleShooting()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            Debug.Log("SOL TIK BASILDI!");
            if (animator != null)
            {
                animator.SetTrigger("Shoot");
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
        {
            CameraController camControl = FindFirstObjectByType<CameraController>();
            if (camControl != null)
            {
                camControl.target = this.transform;
            }
        }
    }  
}
