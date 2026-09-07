using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MarketWeaponCard : MonoBehaviour
{
    [Header("Arayüz Bileþenleri")]
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI weaponDamageText;
    public TextMeshProUGUI weaponPriceText;
    public Button buybutton;
    public Image weaponImage;

    private int _weaponId;
    private MarketManager _marketManager;

    public void SetupCard(WeaponDto weapondto,MarketManager manager)
    {
        // API üzerinden gelen veri paketini ve ana market yöneticisinin referansýný karta dahil ediyoruz.
        _marketManager = manager;
        _weaponId=weapondto.id;

        weaponNameText.text = weapondto.name;
        weaponDamageText.text = "Hasar"+" : " + weapondto.damage.ToString();
        weaponPriceText.text="Fiyat"+" : " + weapondto.price.ToString();

        // Resources klasöründeki büyük sprite sheet dosyasýný parçalayýp içindeki tüm alt resimleri bir diziye alýyoruz.
        Sprite[] allSprites = Resources.LoadAll<Sprite>("SilahlarSheet");

        Debug.Log(weapondto.name + " kartý için dosya okundu. Ýçinden çýkan parça sayýsý: " + allSprites.Length);

        // Resim dizisi içinden sadece silahýn ismine tam olarak uyan görseli arayýp buluyoruz.
        Sprite matchingSprite = System.Array.Find(allSprites, s => s.name == weapondto.name);

        if (matchingSprite == null)
        {
            Debug.LogError("HATA: Ýçeride '" + weapondto.name + "' adýnda bir parça BULUNAMADI! Ýsimler uyuþmuyor.");
        }
        else if (weaponImage == null)
        {
            Debug.LogError("HATA: Prefab'daki weaponImage yuvasý boþ býrakýlmýþ!");
        }
        else
        {
            // Eþleþen doðru görseli UI üzerindeki resim bileþenine yansýtýyoruz
            weaponImage.sprite = matchingSprite;
            Debug.Log("BAÞARILI: " + weapondto.name + " resmi karta eklendi!");
        }

        // Butonun hafýzasýndaki eski týklanma dinleyicilerini tamamen temizleyip temiz bir satýn alma görevi ekliyoruz.
        buybutton.onClick.RemoveAllListeners();
        buybutton.onClick.AddListener(OnBuyButtonClicked);
    }

    private void OnBuyButtonClicked()
    {
        // Satýn al butonuna basýldýðýnda karta özel olan silah ID deðerini MarketManager tarafýna iletiyoruz.
        _marketManager.BuyWeaponRequest(_weaponId);
    }
}
