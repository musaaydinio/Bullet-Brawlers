using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;
public class Bullet : NetworkBehaviour
{
    [SerializeField] private float speed = 25f;
    [SerializeField] private float lifeTime = 3f;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
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

        DestroyBullet();
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
