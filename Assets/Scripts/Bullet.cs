using UnityEngine;
using Unity.Netcode;

public class Bullet : NetworkBehaviour
{
    [SerializeField] private float speed = 25f;
    [SerializeField] private int damageAmount = 20;
    [SerializeField] private float lifeTime = 5f;

    private Rigidbody rb;
    private ulong ownerClientId;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void SetOwner(ulong clientId)
    {
        ownerClientId = clientId;
    }

    public override void OnNetworkSpawn()
    {
        rb.linearVelocity = transform.forward * speed;

        if (IsServer)
        {
            Invoke(nameof(DestroyBullet), lifeTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        // Vurulan objenin veya ebeveyninin PlayerHealth scriptini bul
        PlayerHealth targetHealth = other.GetComponentInParent<PlayerHealth>();
        if (targetHealth == null)
        {
            targetHealth = other.GetComponent<PlayerHealth>();
        }

        if (targetHealth != null)
        {
            // Kendi attýðýmýz mermi bize çarparsa ÝPTAL ET (Kendi kendini vuramazsýn)
            if (targetHealth.OwnerClientId == ownerClientId)
            {
                return;
            }

            // Doðru hedefe hasar ver ve konsola yazdýr
            Debug.Log($"Mermiyi Atan: {ownerClientId} -> Vurulan Oyuncu: {targetHealth.OwnerClientId}");
            targetHealth.TakeDamage(damageAmount);
            DestroyBullet();
            return;
        }

        // Duvara veya zemine çarptýysa sil
        DestroyBullet();
    }

    private void DestroyBullet()
    {
        CancelInvoke(nameof(DestroyBullet));

        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
}