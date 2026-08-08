using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    private CharacterController controller;
    private Animator animator;

    [Header("Hareket Ayarlarý")]
    public float speed = 5f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.2f;
    private Vector3 velocity;
    public float mouseSensitivity = 100f;

    [Header("Ateþ Etme Ayarlarý")]
    public GameObject bulletPrefab;
    public Transform firePoint;

    private CameraController camControl;
  
    public NetworkVariable<float> networkedSpeed = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<bool> networkedGrounded = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (controller == null)
        {
            controller = GetComponent<CharacterController>();
        }

        Camera myCam = GetComponentInChildren<Camera>();
        AudioListener myListener = GetComponentInChildren<AudioListener>();

        if (IsOwner)
        {            
            if (controller != null) controller.enabled = true;
            if (myCam != null) myCam.enabled = true;
            if (myListener != null) myListener.enabled = true;

            camControl = GetComponentInChildren<CameraController>();
            if (camControl != null) camControl.target = this.transform;
        }
        else
        {
            if (controller != null) controller.enabled = false;
            if (myCam != null) myCam.enabled = false;
            if (myListener != null) myListener.enabled = false;
        }
    }

    private void Update()
    {      
        if (!IsOwner)
        {
            if (animator != null)
            {              
                animator.SetFloat("Speed", networkedSpeed.Value);
                animator.SetBool("isGrounded", networkedGrounded.Value);
            }
            return;
        }

        MovePlayer();
        HandleShooting();
    }

    private void MovePlayer()
    {
        if (controller == null || !controller.enabled) return;

        bool isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;

        controller.Move(move * speed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);

        float currentMoveMagnitude = move.magnitude;
        
        UpdateAnimatorServerRpc(currentMoveMagnitude, isGrounded);

        if (animator != null)
        {
            animator.SetFloat("Speed", currentMoveMagnitude);
            animator.SetBool("isGrounded", isGrounded);
        }
    }

    [ServerRpc]
    private void UpdateAnimatorServerRpc(float newSpeed, bool newGrounded)
    {
        networkedSpeed.Value = newSpeed;
        networkedGrounded.Value = newGrounded;
    }

    private void HandleShooting()
    {
        if (Input.GetButtonDown("Fire1"))
        {
            if (animator != null) animator.SetTrigger("Shoot");

            if (firePoint != null)
            {
                ShootServerRpc(firePoint.position, firePoint.rotation,OwnerClientId);
            }
        }
    }

    [ServerRpc]
    private void ShootServerRpc(Vector3 spawnPosition, Quaternion spawnRotation,ulong shooterClient)
    {
        if (bulletPrefab == null) return;

        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, spawnRotation);

        if (bullet.TryGetComponent<Bullet>(out var bulletScprit))
        {
            bulletScprit.SetOwner(shooterClient);
        }
        if (bullet.TryGetComponent<NetworkObject>(out var netObj))
        {
            netObj.Spawn();
        }
    }
}