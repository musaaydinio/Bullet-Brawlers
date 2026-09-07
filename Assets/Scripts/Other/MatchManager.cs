using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class MatchManager : NetworkBehaviour
{
    public static MatchManager _instance;

    public NetworkVariable<float> kalansure = new NetworkVariable<float>(605f);
    public NetworkVariable<bool> macBitti = new NetworkVariable<bool>(false);

    public int killsýnýrý = 30;

    private void Awake()
    {
        if (_instance == null) _instance = this;
        else Destroy(gameObject);       
    }

    private void Update()
    {
        if (IsServer && !macBitti.Value)
        {
            kalansure.Value -= Time.deltaTime;

            if (kalansure.Value <= 0)
            {
                MacBittiServerRpc();
            }
        }
    }

    public void KillSiniriKontorl(int killsayisi)
    {
        if (IsServer && !macBitti.Value && killsayisi >= killsýnýrý)
        {
            MacBittiServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void MacBittiServerRpc()
    {
        if (macBitti.Value) return;
        macBitti.Value = true;
        OyunuBitirClientRpc();
    }

    [ClientRpc]
    public void OyunuBitirClientRpc()
    {
        // 1. Ýmleci serbest býrak
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 2. Sahnedeki oyuncularý tara
        PlayerScore[] tumSkorlar = FindObjectsByType<PlayerScore>(FindObjectsSortMode.None);

        foreach (var scoreScript in tumSkorlar)
        {
            if (scoreScript.IsOwner)
            {
                // 3. Kill sayýsýný al ve coini hesapla (Her Kill = 5 Coin)
                int alinanKill = scoreScript.killSayisi.Value;
                int kazanilanCoin = alinanKill * 5;

                Debug.Log($"[MAÇ BÝTTÝ] Toplam Kill: {alinanKill} | Kazanýlan Coin: {kazanilanCoin}");

                // 4. Ödül panelini aç
                PlayerUýManager uiManager = scoreScript.GetComponent<PlayerUýManager>();
                if (uiManager != null)
                {
                    uiManager.OdulPaneliniAc(alinanKill, kazanilanCoin);
                }

                // 5. Doðrudan bu script içinden Web API'ye coin'i gönder
                if (kazanilanCoin > 0)
                {
                    StartCoroutine(AddCoinsCoroutine(kazanilanCoin));
                }

                break;
            }
        }
    }

    // --- COÝN GÖNDERME ÝÞLEMÝ (MATCHMANAGER ÝÇÝNDE) ---

    [System.Serializable]
    public class AddCoinRequestDto
    {
        public int EarnedCoins;
    }

    private IEnumerator AddCoinsCoroutine(int earnedCoins)
    {
        string coinUrl = "https://192.168.1.103:7023/api/Market/add-coins";

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