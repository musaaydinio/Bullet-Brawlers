using UnityEngine;
using Unity.Netcode;

// Að üzerinde hareket eden merminin fiziksel hýzýný, ömrünü ve oyunculara vereceði hasar mekaniklerini yönetiyoruz.
public class Bullet : NetworkBehaviour
{
    public float speed = 80f;
    public int damageAmount = 20;
    public float lifeTime = 20f;

    private Rigidbody rb;
    private ulong ownerClientId;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void SetOwner(ulong clientId)
    {
        // Merminin hangi oyuncu tarafýndan ateþlendiðini kaydediyoruz.
        ownerClientId = clientId;
    }

    public override void OnNetworkSpawn()
    {
        // Mermi sahnede belirdiðinde fiziksel hýzýný ileri yönlü olarak tetikliyoruz.
        if (rb != null)
        {
            rb.linearVelocity = transform.forward * speed;
        }
        // Merminin sýnýrsýz süre sahnede kalýp performans düþürmemesi için sadece sunucu tarafýnda yok olma süresi baþlatýyoruz.
        if (IsServer)
        {
            Invoke(nameof(DestroyBullet), lifeTime);
        }
    }

    [ClientRpc]
    public void InitializeBulletClientRpc(float networkedSpeed, int networkedDamage)
    {
        // Sunucudan gelen güncel hýz ve hasar deðerlerini istemcilerde eþitliyoruz.
        speed = networkedSpeed;
        damageAmount = networkedDamage;

        if (rb != null)
        {
            rb.linearVelocity = transform.forward * speed;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Çarpýþma ve hasar hesaplamalarý hilelerin önüne geçmek için yalnýzca sunucu tarafýnda iþlenir.
        if (!IsServer) return;

        PlayerHealth targetHealth = other.GetComponentInParent<PlayerHealth>();
        if (targetHealth == null)
        {
            targetHealth = other.GetComponent<PlayerHealth>();
        }

        if (targetHealth != null)
        {
            // Oyuncunun kendi sýktýðý merminin kendine hasar vermesini engelliyoruz.
            if (targetHealth.OwnerClientId == ownerClientId)
            {
                return;
            }
            // Hedefin canýný azaltýp vuran kiþinin kimliðini bildiriyoruz.
            targetHealth.TakeDamage(damageAmount, ownerClientId);

            DestroyBullet();
            return;
        }

        DestroyBullet();
    }

    private void DestroyBullet()
    {
        CancelInvoke(nameof(DestroyBullet));
        // Mermi objesini að üzerindeki tüm istemcilerden güvenli bir þekilde kaldýrýyoruz.
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }
    }
}