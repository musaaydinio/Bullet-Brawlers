using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    public TextMeshProUGUI weaponNameText;
    public Image weaponImage;
    public Button kusanButonu;
    public TextMeshProUGUI bildirimText;

    private int _weaponId;

    public void SetWeaponData(int id, string name)
    {
        // API'den gelen silah bilgisine göre slotun içeriðini dolduruyoruz.
        _weaponId = id;
        weaponNameText.text = name;

        Sprite[] allSprites = Resources.LoadAll<Sprite>("SilahlarSheet");

        // Ýsmi API'den gelenle eþleþen sprite parçasýný buluyoruz
        Sprite matchingSprite = System.Array.Find(allSprites, s => s.name == name);

        // Görsel hatalarýný önlemek için güvenlik kontrollerini yapýyoruz. Eksik resim veya UI bileþeni varsa konsola log düþüyoruz.
        if (matchingSprite == null)
        {
            Debug.LogError("ENVANTER HATA: '" + name + "' adýnda bir resim SilahlarSheet içinde BULUNAMADI!");
        }
        else if (weaponImage == null)
        {
            Debug.LogError("ENVANTER HATA: Prefab üzerindeki Silah Resmi Image yuvasý boþ býrakýlmýþ!");
        }
        else
        {
            // Her þey yolundaysa resmi ekrana basýyoruz
            weaponImage.sprite = matchingSprite;
        }

        // Slot her oluþturulduðunda eski týklama görevlerini temizleyip temiz bir kuþanma eventi baðlýyoruz.
        if (kusanButonu != null)
        {
            kusanButonu.onClick.RemoveAllListeners();
            kusanButonu.onClick.AddListener(KusanButonunaBasildi);
        }
        else
        {
            Debug.LogError("ENVANTER HATA: Prefab üzerindeki Kuþan Butonu yuvasý boþ býrakýlmýþ!");
        }
    }
    private void KusanButonunaBasildi()
    {
        string secilenSilahAdi = weaponNameText.text;

        // Maça girerken ana karakterin hangi silahý elinde tutacaðýný belirlemek için seçimi hafýzaya kaydediyoruz.
        PlayerPrefs.SetString("KusanilanSilah", secilenSilahAdi);
        PlayerPrefs.Save();

        Debug.Log("Lobi: Maça girerken kullanýlacak silah hafýzaya alýndý -> " + secilenSilahAdi);

        if (bildirimText != null)
        {
            bildirimText.text = "Silah kuþanýldý!";
            bildirimText.color = Color.green;
        }
    }
}


 

