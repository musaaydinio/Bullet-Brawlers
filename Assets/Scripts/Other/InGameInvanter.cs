using TMPro;
using Unity.Netcode;
using UnityEngine;

// Oyun içi (maç esnasýnda) silah deðiþtirme ve envanter arayüzünü yöneten istemci taraflý bileþen
public class PlayerInGameInventory : NetworkBehaviour
{
    [Header("UI Panelleri")]
    public GameObject envanterPanel;
    public Transform inventoryContentParent;
    public GameObject inventoryItemPrefab;
    public TextMeshProUGUI bTusuBilgiText;

    [Header("Ayarlar")]
    public float bTusuSuresi = 5f;
    private float izinVerilenSonZaman = 0f;
    private bool sureDolduMu = true;

    private void Start()
    {
        if (envanterPanel != null) envanterPanel.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        // Yalnýzca karakterin yerel sahibi (Local Owner) kendi envanter sayacýný baþlatabilir
        if (IsOwner)
        {
            YenidenDogusSuresiniBaslat();
        }
    }

    // Oyuncu doðduðunda veya maça baþladýðýnda belirli bir süre (5 sn) silah deðiþtirme izni verir
    public void YenidenDogusSuresiniBaslat()
    {
        if (!IsOwner) return;

        izinVerilenSonZaman = Time.time + bTusuSuresi;
        sureDolduMu = false;

        if (envanterPanel != null) envanterPanel.SetActive(false);

        if (bTusuBilgiText != null)
        {
            bTusuBilgiText.text = "B tuþuna basýn ve silah deðiþtirin";
            bTusuBilgiText.gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        // Sadece ilgili karakterin sahibi olan istemci girdiði girdileri (Input) iþler
        if (!IsOwner || sureDolduMu) return;

        // Ýzin verilen zaman penceresi içinde 'B' tuþuna basýlýrsa envanter paneli açýlýr/kapanýr
        if (Time.time <= izinVerilenSonZaman)
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                bool suAnAcikMi = envanterPanel.activeSelf;
                envanterPanel.SetActive(!suAnAcikMi);

                if (!suAnAcikMi)
                {
                    // Envanter açýldýðýnda imleci serbest býrakýp týklanabilir yapýyoruz
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    OyunIciEnvanteriDoldur();

                    if (bTusuBilgiText != null) bTusuBilgiText.gameObject.SetActive(false);
                }
                else
                {
                    // Envanter kapatýldýðýnda imleci tekrar merkeze kilitliyoruz (FPS kamera kontrolü)
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;

                    if (bTusuBilgiText != null) bTusuBilgiText.gameObject.SetActive(true);
                }
            }
        }
        else
        {
            // Zaman penceresi dolduðunda envanteri zorla kapatýp imleci kilitliyoruz
            sureDolduMu = true;
            if (envanterPanel != null && envanterPanel.activeSelf)
            {
                envanterPanel.SetActive(false);
            }

            if (bTusuBilgiText != null) bTusuBilgiText.gameObject.SetActive(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // Oyuncunun sahip olduðu silahlarý dinamik olarak UI slotlarýna aktarýr
    private void OyunIciEnvanteriDoldur()
    {
        foreach (Transform child in inventoryContentParent)
        {
            Destroy(child.gameObject);
        }

        string kayitliSilahlar = PlayerPrefs.GetString("BenimSilahlarim", "");
        string[] silahListesi = kayitliSilahlar.Split(',');

        foreach (string silahAdi in silahListesi)
        {
            if (string.IsNullOrEmpty(silahAdi)) continue;

            GameObject yeniSlot = Instantiate(inventoryItemPrefab, inventoryContentParent, false);
            InventorySlot slotScript = yeniSlot.GetComponent<InventorySlot>();

            if (slotScript != null)
            {
                slotScript.SetWeaponData(0, silahAdi);

                // Silah seçildiðinde sunucuya ServerRpc gönderilerek silah deðiþimi sunucu yetkisinde yapýlmasý saðlanýr
                slotScript.kusanButonu.onClick.AddListener(() =>
                {
                    if (TryGetComponent<PlayerMovement>(out var movement))
                    {
                        movement.SilahDegistirServerRpc(silahAdi);
                    }
                });
            }
        }
    }
}