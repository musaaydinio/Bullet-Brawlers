using System.Collections;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("UI Elemanlarý")]
    [SerializeField] private Button btnStartHost;
    [SerializeField] private Button btnJoinClient;
    [SerializeField] private TMP_InputField inputFieldIP;

    [Header("UI Panelleri (Menü / Market / Envanter)")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject marketPanel;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject settingPanel;

    [Header("Ayarlar")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private ushort defaultPort = 7777;

    [Header("Sol Üst UI Bilgileri")]
    public TextMeshProUGUI oyuncuAdiText;
    public TextMeshProUGUI altinText;
    public TextMeshProUGUI uyariText;


    private readonly string userInfoUrl = "https://192.168.1.103:7023/api/Market/me";

    private UnityTransport transport;

    private void Start()
    {
        //Að yöneticisi sahnede mevcutsa baðlantý kopma durumlarý için ilgili event dinlenir.
        if (NetworkManager.Singleton != null)
        {
            transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        btnStartHost.onClick.AddListener(OnStartHostClicked);
        btnJoinClient.onClick.AddListener(OnJoinClientClicked);
        ShowMainMenu();
        APIDenBilgileriCek();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnStartHostClicked()
    {
        if (NetworkManager.Singleton == null) return;

        // Silahsýz oyuna giriþ engellendi. Envanterden silah seçimi zorunlu tutuldu.
        string kusanilanSilah = PlayerPrefs.GetString("KusanilanSilah", "");
        if (kusanilanSilah == "") 
        {
            if (uyariText != null)
            {
                uyariText.color = Color.red;
                uyariText.text = "Oyuna girmeden önce envanterden bir silah kuþanmalýsýn!";
            }
            Debug.LogWarning("Silah kuþanýlmadý, sunucu açma isteði REDDEDÝLDÝ.");

            SetUIInteractable(true);
            return; 
        }

        if (uyariText != null) uyariText.text = "";

        // Ýþlem sürerken art arda butona basýlýp aðýn çökertilmesini önlemek amacýyla arayüz kilitlenir.
        SetUIInteractable(false);

        if (transport != null)
        {
            transport.SetConnectionData("127.0.0.1", defaultPort, "0.0.0.0");
        }

        if (NetworkManager.Singleton.StartHost())
        {
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("Host baþlatýlamadý!");
            SetUIInteractable(true);
        }
    }

    private void OnJoinClientClicked()
    {
        if (NetworkManager.Singleton == null) return;
        // Baðlanan oyuncunun silahsýz oyuna girmesi clientde de engellendi.
        string kusanilanSilah = PlayerPrefs.GetString("KusanilanSilah", "");
        if (kusanilanSilah == "") 
        {
            if (uyariText != null)
            {
                uyariText.color = Color.red;
                uyariText.text = "Oyuna girmeden önce envanterden bir silah kuþanmalýsýn!";
            }
            Debug.LogWarning("Silah kuþanýlmadý, sunucu açma isteði REDDEDÝLDÝ.");
         
            SetUIInteractable(true);
            return; 
        }

        if (uyariText != null) uyariText.text = "";

        string targetIP = string.IsNullOrEmpty(inputFieldIP.text) ? "127.0.0.1" : inputFieldIP.text.Trim();

        if (transport != null)
        {
            transport.SetConnectionData(targetIP, defaultPort);
        }

        SetUIInteractable(false);

        bool success = NetworkManager.Singleton.StartClient();

        if (!success)
        {
            Debug.LogError("Client baþlatma isteði baþarýsýz oldu.");
            ResetNetworkAndUI();
        }
    }


    private void OnClientDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsHost)
        {
            Debug.LogWarning("Sunucuya baðlanýlamadý veya baðlantý kesildi!");
            ResetNetworkAndUI();
        }
    }

    private void ResetNetworkAndUI()
    {
        // Baðlantý reddedilirse veya koparsa oyuncunun menüde hapis kalmamasý için að temizlenir ve UI kilidi açýlýr.
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SetUIInteractable(true);
    }

    private void SetUIInteractable(bool state)
    {
        if (settingPanel != null) settingPanel.SetActive(false);
        if (btnStartHost != null) btnStartHost.interactable = state;
        if (btnJoinClient != null) btnJoinClient.interactable = state;
        if (inputFieldIP != null) inputFieldIP.interactable = state;
    }
    public void ShowMainMenu()
    {
        if (settingPanel != null) settingPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (marketPanel != null) marketPanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
    }

    public void OpenMarketPanel()
    {
        if (settingPanel != null) settingPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (marketPanel != null) marketPanel.SetActive(true);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
    }

    public void OpenInventoryPanel()
    {
        if (settingPanel != null) settingPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (marketPanel != null) marketPanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(true);
    }
    public void OpenSetiingPanel()
    {
        if(settingPanel != null) settingPanel.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (marketPanel != null) marketPanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
    }
    public void APIDenBilgileriCek()
    {
        StartCoroutine(KullaniciBilgileriniGetirCoroutine());
    }
    private IEnumerator KullaniciBilgileriniGetirCoroutine()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(userInfoUrl))
        {
            request.certificateHandler = new BypassCertificate();
            
            request.disposeCertificateHandlerOnDispose = true;

            // Login ekranýnda belleðe alýnan JWT Token deðeri Authorization baþlýðýna eklenir. 
            // Token eksik veya hatalýysa API 401 hatasý döndürür.
            if (!string.IsNullOrEmpty(SessionManager.Token))
            {
                request.SetRequestHeader("Authorization", "Bearer " + SessionManager.Token);
            }
            else
            {
                Debug.LogError("DÝKKAT: TOKEN BOÞ! API seni bu yüzden 401 ile kapýdan kovuyor.");
            }

            yield return request.SendWebRequest();

            Debug.Log("API DURUM KODU: " + request.responseCode);
            Debug.Log("API HAM CEVAP: " + request.downloadHandler.text);

            if (request.result == UnityWebRequest.Result.Success)
            {
                // API'den dönen JSON verisi modele dönüþtürülerek UI üzerindeki metin alanlarýna yansýtýlýr.
                UserInfoDto userInfo = JsonUtility.FromJson<UserInfoDto>(request.downloadHandler.text);

                oyuncuAdiText.text = userInfo.userName;
                altinText.text = "Altýn: " + userInfo.coins;

                // Skor tablosu ve diðer mekaniklerde kullanýlmak üzere oyuncu nicki PlayerPrefs üzerine kaydedilir.
                PlayerPrefs.SetString("PlayerName", userInfo.userName);
            }
        }
    }
}


