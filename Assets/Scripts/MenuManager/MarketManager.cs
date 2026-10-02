using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class MarketManager : MonoBehaviour
{
    [Header("Market Ayarlarý")]
    public GameObject marketWeaponCardPrefab; 
    public Transform scrollContentPanel;

    [Header("Satýn Alma UI")]
    public TextMeshProUGUI sistemMesajiText; 
    public MenuManager menuManager;

    [Header("Envanter Ayarlarý")]
    public Transform inventoryContentParent; 
    public GameObject inventoryItemPrefab;

    private readonly string baseurl= "https://gamebackendapi-difs.onrender.com/api/Market";

    private void Start()
    {
        StartCoroutine(GetWeaponsCoroutine());
        StartCoroutine(EnvanteriGetirCoroutine());
    }

    private IEnumerator GetWeaponsCoroutine()
    {
        // API'nin silahlarý listeleyen metoduna GET isteði atýyoruz
        using (UnityWebRequest request = UnityWebRequest.Get(baseurl + "/weapons"))
        {
            // HTTPS localhost engelini aþýyoruz
            request.certificateHandler = new BypassCertificate();

            // Kimlik doðrulama Token'ýmýzý ekliyoruz
            if (!string.IsNullOrEmpty(SessionManager.Token))
                if (!string.IsNullOrEmpty(SessionManager.Token))
                {
                    request.SetRequestHeader("Authorization", "Bearer " + SessionManager.Token);
                }
                else
                {
                    Debug.LogError("DÝKKAT: Token boþ! API'ye giriþ yapmadan istek atýlýyor."); 
                }           
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                Debug.Log("API'den Gelen Silahlar: " + jsonResponse);
                // JSON Metnini Unity'deki C# Objelerine Çeviriyoruz
                WeaponDto[] weapons = JsonHelper.FromJson<WeaponDto>(jsonResponse);
                //PREFAB'LARI EKRANA DÝZÝYORUZ
                foreach (WeaponDto wepaon in weapons)
                {
                    // Her bir silah için Content objesinin içine 1 tane Prefab kopyalýyoruz.
                    Debug.Log("Ýþlenen Silah: " + wepaon.name);
                    GameObject newCard = Instantiate(marketWeaponCardPrefab, scrollContentPanel, false);
                    // Kopyalanan Prefab'ýn içindeki yazýlarý API'den gelen verilerle dolduruyoez.
                    MarketWeaponCard cardScript = newCard.GetComponent<MarketWeaponCard>();
                    cardScript.SetupCard(wepaon, this);
                }
            }
            else
            {
                Debug.LogError("Market listesi çekilemedi: " + request.error);
            }
        }
    }
    public void BuyWeaponRequest(int weaponId)
    {
        Debug.Log("Satýn alýnmak istenen silah ID: " + weaponId);
        StartCoroutine(BuyWeaponCoroutine(weaponId));
    }
    private IEnumerator BuyWeaponCoroutine(int weaponId)
    {
        string buyUrl = baseurl + "/buy";
        // POST isteði için gönderilecek olan JSON payload'u hazýrlýyoruz.
        string jsonPayload = "{\"weaponId\":" + weaponId + "}"; 

        using (UnityWebRequest request = new UnityWebRequest(buyUrl, "POST"))
        {
            // Payload byte dizisine çevrilip isteðe yüklüyoruz.
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.certificateHandler = new BypassCertificate();

            if (!string.IsNullOrEmpty(SessionManager.Token))
            {
                request.SetRequestHeader("Authorization", "Bearer " + SessionManager.Token);
            }

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Satýn alma baþarýlýysa arayüzde oyuncuya bilgi verilir.
                sistemMesajiText.text = "Silah baþarýyla envantere eklendi!";
                sistemMesajiText.color = Color.green;

                // Paranýn düþtüðünü ekranda göstermek için MenuManager'a "Verileri tekrar çek" diyoruz
                if (menuManager != null)
                {
                    menuManager.APIDenBilgileriCek();
                }
                // Satýn alýnan silahýn envanter UI'ýnda hemen görünmesi için envanter listesi yenilenir.
                StartCoroutine(EnvanteriGetirCoroutine());

            }
            else
            {
                Debug.LogError("API'den Gelen Gerçek Hata: " + request.downloadHandler.text);
                Debug.LogError("Hata Kodu: " + request.responseCode);
                // 400 BAD REQUEST: Para yetersiz!
                sistemMesajiText.text = "Yetersiz bakiye!";
                sistemMesajiText.color = Color.red;
            }
        }
    }
    public IEnumerator EnvanteriGetirCoroutine()
    {
        string url = "https://gamebackendapi-difs.onrender.com/api/Market/inventory";

        using (UnityWebRequest request = new UnityWebRequest(url, "GET"))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.certificateHandler = new BypassCertificate(); 

            
            if (!string.IsNullOrEmpty(SessionManager.Token))
            {
                request.SetRequestHeader("Authorization", "Bearer " + SessionManager.Token);
            }

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // Mevcut UI üst üste binmesin diye liste oluþturulmadan önce ekran temizliyoruz.
                foreach (Transform child in inventoryContentParent)
                {
                    Destroy(child.gameObject);
                }

                // API'den gelen JSON, oyuncunun sahip olduðu silahlar listesine çevriyoruz.
                string jsonCevap = request.downloadHandler.text;
                WeaponDto[] sahipOlunanSilahlar = JsonHelper.FromJson<WeaponDto>(jsonCevap);

                string silahIsimleri = "";
                foreach (WeaponDto silah in sahipOlunanSilahlar) { silahIsimleri += silah.name + ","; }
                PlayerPrefs.SetString("BenimSilahlarim", silahIsimleri);
                PlayerPrefs.Save();

                // Sahip olunan her silah için envanter ekranýnda bir slot oluþturulur.
                foreach (WeaponDto silah in sahipOlunanSilahlar)
                {
                    GameObject yeniSlot = Instantiate(inventoryItemPrefab, inventoryContentParent,false);
                    InventorySlot slotScript = yeniSlot.GetComponent<InventorySlot>();

                    if (slotScript != null)
                    {
                        // Slot UI elementinin içine silahýn ismi ve ID'si iþlenir.
                        slotScript.SetWeaponData(silah.id, silah.name);
                    }
                }
            }
            else
            {
                Debug.LogError("Envanter çekilemedi: " + request.downloadHandler.text);
            }
        }
    }
}


