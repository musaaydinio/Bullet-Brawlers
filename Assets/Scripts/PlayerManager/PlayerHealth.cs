using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private int maxHealth = 100;

    // Can deðeri sadece sunucu(Server) tarafýndan deðiþtirilebilir, ancak tüm oyuncular(Client) tarafýndan okunabilir.
    // Ýstemci taraflý ölümsüzlük hilelerini engellemek için
    public NetworkVariable<int> currentHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Canvas playerCanvas;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {

        if (!IsServer) return;

        // Karakter haritadan aþaðý düþtüyse ve hala yaþýyorsa
        if (transform.position.y <= -1f && currentHealth.Value > 0)
        {
            Debug.Log($"[SÝSTEM] Oyuncu {OwnerClientId} haritadan aþaðý düþtü!");

            // Caný sýfýrlayýp doðrudan öldürüyoruz.
            currentHealth.Value = 0;
            Die();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Can deðiþkeni her deðiþtiðinde UI'ý senkronize etmek için event dinlenir.
        currentHealth.OnValueChanged += OnHealthChanged;

        if (IsServer)
        {
            currentHealth.Value = maxHealth;
        }


        // UI elemanlarý sadece karakterin asýl sahibinde(Local Player) görünür olmalýdýr.
        // Diðer oyuncularýn ekranýnda baþkalarýnýn can barlarý (Canvas) gizlenir.
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

        if (healthText != null)
        {
            healthText.text = health.ToString();
        }
    }

    // Hasar alma iþlemi tamamen sunucu yetkisindedir. Ýstemciden gelen hasar talepleri burada iþlenir.
    public void TakeDamage(int damage, ulong vuranKisiID)
    {
        if (!IsServer) return;
        if (currentHealth.Value <= 0) return;

        currentHealth.Value -= damage;

        if (currentHealth.Value <= 0)
        {
            Debug.Log($"[SÝSTEM] Ölüm Gerçekleþti! Ölen: {OwnerClientId} | Vuran: {vuranKisiID}");

            // Ölüm anýnda sahadaki tüm skor scriptleri taranarak mermiyi sýkan kiþi (vuranKisiID) bulunur.
            // Sadece bir oyuncu öldüðünde çalýþtýðý için performansa yük bindirmez.       
            PlayerScore[] tumSkorlar = FindObjectsByType<PlayerScore>(FindObjectsSortMode.None);

            foreach (var skorScript in tumSkorlar)
            {
                if (skorScript.OwnerClientId == vuranKisiID)
                {
                    skorScript.killSayisi.Value++;
                    Debug.Log($"[SÝSTEM] BAÞARILI! {vuranKisiID} ID'li oyuncunun skoru artýrýldý. Yeni Skor: {skorScript.killSayisi.Value}");
                    break;
                }
            }

            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"Oyuncu {OwnerClientId} öldü!");

        // Ölüm animasyonu ve fiziksel kontrollerin kilitlenmesi aðdaki tüm istemcilere bildirilir.
        PlayDieAnimationClientRpc();
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

        // Ölen karakterin hareket etmesi engellenir.
        var movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;

        var controller = GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
    }
    
    private void Respawn()
    {
        if (!IsServer) return;

        // Sunucu haritadaki spawn noktalarýndan rastgele birini seçer
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("RespawmPoint");
        Vector3 targetPos = Vector3.zero;
        Quaternion targetRot = Quaternion.identity;

        if (spawnPoints.Length > 0)
        {
            int randomIndex = Random.Range(0, spawnPoints.Length);
            targetPos = spawnPoints[randomIndex].transform.position;
            targetRot = spawnPoints[randomIndex].transform.rotation;
        }

        // ClientNetworkTransform yetkisi Client'ta olduðu için 
        // konum deðiþtirme emrini ClientRpc ile doðrudan SAHÝBÝNE gönderiyoruz.
        TeleportToSpawnPointClientRpc(targetPos, targetRot);

        // 3. Sunucuda caný tekrar fulle
        currentHealth.Value = maxHealth;
    }

    [ClientRpc]
    private void TeleportToSpawnPointClientRpc(Vector3 newPos, Quaternion newRot)
    {
        //  HERKESÝN EKRANINDA ÇALIÞACAK KISIM (Host, Client ve Diðer Ýzleyiciler)
        // Önce takýlý kalan "Die" trigger'ýný temizliyoruz ve animasyonu Idle'a çekiyoruz
        if (animator != null)
        {
            animator.ResetTrigger("Die");
            animator.Play("Hareket");
        }

        // Hareket scriptini herkesin ekranýnda tekrar aktif yapýyoruz
        var movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = true;


        // SADECE KARAKTERÝN GERÇEK SAHÝBÝNÝN EKRANINDA ÇALIÞACAK KISIM      
        if (IsOwner)
        {
            CharacterController controller = GetComponent<CharacterController>();

            // Pozisyon ýþýnlanýrken fizikle çakýþmasýn diye controller'ý geçici kapatýyoruz
            if (controller != null) controller.enabled = false;

            transform.position = newPos;
            transform.rotation = newRot;

            if (controller != null) controller.enabled = true;

            GunSetting silahScpt= GetComponentInChildren<GunSetting>();
            if(silahScpt != null)
            {
                silahScpt.MermileriSifirla();
            }
        }
    }
}