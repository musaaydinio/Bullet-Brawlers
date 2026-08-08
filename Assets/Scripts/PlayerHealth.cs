using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 100;

    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [SerializeField] private Slider healthSlider;
    [SerializeField] private Canvas playerCanvas; // Prefab içindeki UI Canvas'ý (Varsa Inspector'dan sürükle)
    private Animator animator;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        currentHealth.OnValueChanged += OnHealthChanged;

        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }

        // CAN BARINI DÜZELTEN KISIM:
        // Karakter bizim deðilse (IsOwner = false), onun UI Canvas'ýný Host/Client ekranýnda kapatýyoruz.
        // Böylece baþkasýnýn can barý senin ekranýnýn üzerine binerel kýrmýzý göstermez.
        if (!IsOwner)
        {
            if (playerCanvas != null) playerCanvas.enabled = false;
        }
        else
        {
            if (playerCanvas != null) playerCanvas.enabled = true;
            UpdateHealthUI(currentHealth.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        currentHealth.OnValueChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int previousValue, int newValue)
    {
        // Sadece lokal oyuncu (IsOwner) kendi ekranýndaki can barýný günceller.
        if (IsOwner)
        {
            UpdateHealthUI(newValue);
        }
    }

    private void UpdateHealthUI(int health)
    {
        if (!IsOwner) return;

        if (healthSlider != null)
        {
            healthSlider.value = (float)health / maxHealth;
        }
    }

    public void TakeDamage(int damage)
    {
        if (!IsServer) return;
        if (currentHealth.Value <= 0) return;

        currentHealth.Value -= damage;

        if (currentHealth.Value <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"Oyuncu {OwnerClientId} öldü!");
        PlayDieAnimationClientRpc();

        // 3 saniye sonra doðma metodunu çaðýrýyoruz
        Invoke(nameof(Respawn), 3f);
    }

    [ClientRpc]
    private void PlayDieAnimationClientRpc()
    {
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }

        var movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;

        var controller = GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
    }

    // CLIENT SPAWN PROBLEMÝNÝ ÇÖZEN KISIM
    private void Respawn()
    {
        if (!IsServer) return;

        // 1. Sunucu haritadaki spawn noktalarýndan rastgele birini seçer
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("RespawmPoint");
        Vector3 targetPos = Vector3.zero;
        Quaternion targetRot = Quaternion.identity;

        if (spawnPoints.Length > 0)
        {
            int randomIndex = Random.Range(0, spawnPoints.Length);
            targetPos = spawnPoints[randomIndex].transform.position;
            targetRot = spawnPoints[randomIndex].transform.rotation;
        }

        // 2. ClientNetworkTransform yetkisi Client'ta olduðu için 
        // konum deðiþtirme emrini ClientRpc ile doðrudan SAHÝBÝNE gönderiyoruz.
        TeleportToSpawnPointClientRpc(targetPos, targetRot);

        // 3. Sunucuda caný tekrar fulle
        currentHealth.Value = maxHealth;
    }

    [ClientRpc]
    private void TeleportToSpawnPointClientRpc(Vector3 newPos, Quaternion newRot)
    {
        // 1. HERKESÝN EKRANINDA ÇALIÞACAK KISIM (Host, Client ve Diðer Ýzleyiciler)
        // Önce takýlý kalan "Die" trigger'ýný temizliyoruz ve animasyonu Idle'a çekiyoruz
        if (animator != null)
        {
            animator.ResetTrigger("Die");
            animator.Play("Hareket"); // Animator'ündeki varsayýlan durumun adý neyse onu yaz (örn: "Idle", "Movement" vs.)
        }

        // Hareket scriptini herkesin ekranýnda tekrar aktif yapýyoruz
        var movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = true;


        // 2. SADECE KARAKTERÝN GERÇEK SAHÝBÝNÝN (IsOwner) EKRANINDA ÇALIÞACAK KISIM
        // ClientNetworkTransform kullandýðýmýz için pozisyonu sadece sahibi deðiþtirebilir
        if (IsOwner)
        {
            CharacterController controller = GetComponent<CharacterController>();

            // Pozisyon ýþýnlanýrken fizikle çakýþmasýn diye controller'ý geçici kapatýyoruz
            if (controller != null) controller.enabled = false;

            transform.position = newPos;
            transform.rotation = newRot;

            if (controller != null) controller.enabled = true;
        }
    }
}