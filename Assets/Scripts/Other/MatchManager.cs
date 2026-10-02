using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

// Maç döngüsünü, skoru, galibiyet þartlarýný ve maç sonu ekonomisini (REST API) yöneten merkezi sunucu yöneticisi
public class MatchManager : NetworkBehaviour
{
    public static MatchManager _instance;

    // Að üzerindeki tüm istemcilerde otomatik olarak senkronize olan maç süresi ve maç durum deðiþkenleri
    public NetworkVariable<float> kalansure = new NetworkVariable<float>(605f);
    public NetworkVariable<bool> macBitti = new NetworkVariable<bool>(false);

    public int killSiniri = 30;

    private void Awake()
    {
        if (_instance == null) _instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        // Maç süresi takibi tamamen Sunucu Yetkilidir (Server-Authoritative); istemciler süreyi hileyle deðiþtiremez
        if (IsServer && !macBitti.Value)
        {
            kalansure.Value -= Time.deltaTime;

            if (kalansure.Value <= 0)
            {
                MacBittiServerRpc();
            }
        }
    }

    // Skor sýnýrýna ulaþýldýðýnda sunucu tarafýndan çaðrýlan zafer kontrolü
    public void KillSiniriKontorl(int killsayisi)
    {
        if (IsServer && !macBitti.Value && killsayisi >= killSiniri)
        {
            MacBittiServerRpc();
        }
    }

    // Maç bitiþ durumunu doðrulayan ve tüm istemcilere duyuran ServerRpc
    [ServerRpc(RequireOwnership = false)]
    public void MacBittiServerRpc()
    {
        if (macBitti.Value) return;
        macBitti.Value = true;
        OyunuBitirClientRpc();
    }

    // Tüm istemcilerde (Clients) eþ zamanlý çalýþan, ödül panelini açan ve cüzdan güncellemesini tetikleyen ClientRpc
    [ClientRpc]
    public void OyunuBitirClientRpc()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PlayerScore[] tumSkorlar = FindObjectsByType<PlayerScore>(FindObjectsSortMode.None);

        foreach (var scoreScript in tumSkorlar)
        {
            if (scoreScript.IsOwner)
            {
                int alinanKill = scoreScript.killSayisi.Value;
                int kazanilanCoin = alinanKill * 30;

                Debug.Log($"[MAÇ BÝTTÝ] Toplam Kill: {alinanKill} | Kazanýlan Coin: {kazanilanCoin}");

                PlayerUýManager uiManager = scoreScript.GetComponent<PlayerUýManager>();
                if (uiManager != null)
                {
                    uiManager.OdulPaneliniAc(alinanKill, kazanilanCoin);
                }

                // Kazanýlan coin miktarý 0'dan büyükse doðrudan ASP.NET Core Web API sunucusuna POST isteði gönderilir
                if (kazanilanCoin > 0)
                {
                    StartCoroutine(AddCoinsCoroutine(kazanilanCoin));
                }

                break;
            }
        }
    }

    [System.Serializable]
    public class AddCoinRequestDto
    {
        public int EarnedCoins;
    }

    // Maç sonucunda kazanýlan altýn miktarýný güvenli bir þekilde Web API veritabanýna iþleyen asenkron istek
    private IEnumerator AddCoinsCoroutine(int earnedCoins)
    {
        string coinUrl = "https://gamebackendapi-difs.onrender.com/api/Market/add-coins";

        AddCoinRequestDto coinData = new AddCoinRequestDto
        {
            EarnedCoins = earnedCoins
        };

        string jsonData = JsonUtility.ToJson(coinData);

        using (UnityWebRequest request = new UnityWebRequest(coinUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.certificateHandler = new BypassCertificate();

            // Ýstemcinin oturum açarken elde ettiði JWT Bearer Token eklenerek yetkili istek atýlýr
            if (!string.IsNullOrEmpty(SessionManager.Token))
            {
                request.SetRequestHeader("Authorization", "Bearer " + SessionManager.Token);
            }

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Coin eklenemedi: " + request.error + " | " + request.downloadHandler.text);
            }
            else
            {
                Debug.Log("Coinler baþarýyla hesaba eklendi! Cevap: " + request.downloadHandler.text);
            }
        }
    }
}