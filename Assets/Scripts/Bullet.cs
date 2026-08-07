using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;
public class Bullet : NetworkBehaviour
{
    [SerializeField] private float speed = 25f;
    [SerializeField] private int damageAmount = 20;
    [SerializeField] private float lifeTime = 3f;

    private Rigidbody rb;
    private ulong ownerClientId;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void SetOwner(ulong clientId)
    {
        ownerClientId= clientId;
    }
    public override void OnNetworkSpawn()
    {
        rb.linearVelocity=transform.forward*speed;

        if (IsServer)
        {
            Invoke(nameof(DestroyBullet), lifeTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if(other.TryGetComponent<PlayerHealth>(out var targetHealth))
        {
            if(targetHealth.OwnerClientId != ownerClientId)
            {
                targetHealth.TakeDamge(damageAmount);
                DestroyBullet();
            }
        }
        else if(other.CompareTag("Bullet"))
        {
            DestroyBullet();
        }
    }

    private void DestroyBullet()
    {
        CancelInvoke (nameof(DestroyBullet));   

        if(NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
}
