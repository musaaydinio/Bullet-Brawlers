using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;        
using Unity.Services.Lobbies.Models; 
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Ana menü navigasyonunu, UGS (Relay & Lobby) servislerini ve ASP.NET Core REST API entegrasyonunu yöneten ana sýnýf
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
    [SerializeField] private string LobbySceneName = "LobbyScene";

    [Header(" UI Bilgileri")]
    public TextMeshProUGUI oyuncuAdiText;
    public TextMeshProUGUI altinText;
    public TextMeshProUGUI uyariText;

    [Header("Oda Listesi Eklentileri (YENÝ)")]
    [SerializeField] private GameObject roomListPanel;
    [SerializeField] private Transform roomListContainer;
    [SerializeField] private GameObject roomItemPrefab;
    [SerializeField] private Button btnRefreshLobbies;
    [SerializeField] private int maxPlayers = 4;

    // Web API endpoint adresi
    private readonly string userInfoUrl = "https://gamebackendapi-difs.onrender.com/api/Market/me";
    private UnityTransport transport;

    private async void Start()
    {
        // Unity Bulut Servislerini Baþlat
        await InitializeUnityServicesAsync();

        // Að yöneticisi sahnede mevcutsa baðlantý kopma durumlarý için ilgili event dinlenir.
        if (NetworkManager.Singleton != null)
        {
            transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        btnStartHost.onClick.AddListener(OnStartHostClicked);
        btnJoinClient.onClick.AddListener(OnJoinClientClicked);
        if (btnRefreshLobbies != null) btnRefreshLobbies.onClick.AddListener(RefreshLobbyList);

        ShowMainMenu();

        // Backend API üzerinden oyuncunun güncel cüzdan ve kullanýcý bilgilerini çekme
        APIDenBilgileriCek();
    }

    // UGS altyapýsýný ve anonim kimlik doðrulama oturumunu baþlatýr
    private async Task InitializeUnityServicesAsync()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UGS] Servis baþlatma hatasý: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        // Bellek sýzýntýlarýný önlemek için event aboneliðinin kaldýrýlmasý
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    // Odayý kuran (Host) tarafýn Relay sunucusu alýp oda ilanýný Lobby servisine açmasý süreci
    private async void OnStartHostClicked()
    {
        if (NetworkManager.Singleton == null) return;

        // Ýstemci tarafý doðrulama: Silahsýz oyuna giriþ engellenir
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

        if (uyariText != null) uyariText.text = "Oda kuruluyor...";
        SetUIInteractable(false);

        try
        {
            // Unity Relay üzerinde bant geniþliði (Allocation) tahsis edilir
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // Üretilen 6 haneli kodu Lobi sahnesi için saklýyoruz
            LobbyDataHolder.CurrentJoinCode = joinCode;

            var relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");
            if (transport != null)
            {
                transport.SetRelayServerData(relayServerData);
            }

            // UGS Lobby servisine oda bilgileri ve JOIN_CODE parametresi eklenerek herkese açýk ilan açýlýr
            string playerName = PlayerPrefs.GetString("PlayerName", "Oyuncu");
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    { "JOIN_CODE", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
                }
            };

            await LobbyService.Instance.CreateLobbyAsync($"{playerName}'in Odasý", maxPlayers, options);

            // Netcode Host sunucusunun baþlatýlmasý ve senkronize lobi sahnesine geçiþ
            if (NetworkManager.Singleton.StartHost())
            {
                NetworkManager.Singleton.SceneManager.LoadScene(LobbySceneName, LoadSceneMode.Single);
            }
            else
            {
                Debug.LogError("Host baþlatýlamadý!");
                SetUIInteractable(true);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Oda kurma hatasý: {e.Message}");
            if (uyariText != null) uyariText.text = "Oda kurulamadý!";
            SetUIInteractable(true);
        }
    }

    // Oda kodu girerek doðrudan maça katýlma isteði
    private async void OnJoinClientClicked()
    {
        if (NetworkManager.Singleton == null) return;

        // Katýlmak isteyen oyuncunun da silah kuþanýp kuþanmadýðý doðrulanýr
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

        string targetCode = inputFieldIP != null ? inputFieldIP.text.Trim() : "";

        if (string.IsNullOrEmpty(targetCode))
        {
            if (uyariText != null) uyariText.text = "Lütfen geçerli bir Oda Kodu girin!";
            return;
        }

        await JoinRelayWithCode(targetCode);
    }

    // Relay katýlým koduyla sunucuya DTLS protokolü üzerinden baðlanma süreci
    public async Task JoinRelayWithCode(string joinCode)
    {
        SetUIInteractable(false);
        if (uyariText != null) uyariText.text = "Odaya baðlanýlýyor...";

        try
        {
            LobbyDataHolder.CurrentJoinCode = joinCode;

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            var relayServerData = AllocationUtils.ToRelayServerData(joinAllocation, "dtls");

            if (transport != null)
            {
                transport.SetRelayServerData(relayServerData);
            }

            // Netcode istemcisinin (Client) baþlatýlmasý
            bool success = NetworkManager.Singleton.StartClient();

            if (!success)
            {
                Debug.LogError("Client baþlatma isteði baþarýsýz oldu.");
                ResetNetworkAndUI();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Odaya katýlma hatasý: {e.Message}");
            if (uyariText != null) uyariText.text = "Odaya katýlým baþarýsýz!";
            ResetNetworkAndUI();
        }
    }

    // Aktif lobileri UGS üzerinden sorgulayýp UI üzerinde dinamik listeleme yöntemi
    public async void RefreshLobbyList()
    {
        if (roomListContainer == null || roomItemPrefab == null) return;

        try
        {
            // Sadece boþ yeri olan (AvailableSlots > 0) odalarý getiren arama filtresi
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Count = 15,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                }
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);

            // Önceki liste elemanlarýnýn temizlenmesi
            foreach (Transform child in roomListContainer)
            {
                Destroy(child.gameObject);
            }

            // Çekilen her bir lobi için prefab türetip eventlerin baðlanmasý
            foreach (Lobby lobby in response.Results)
            {
                GameObject roomItem = Instantiate(roomItemPrefab, roomListContainer);

                TextMeshProUGUI roomNameText = roomItem.transform.Find("RoomNameText")?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI playerCountText = roomItem.transform.Find("PlayerCountText")?.GetComponent<TextMeshProUGUI>();
                Button joinButton = roomItem.transform.Find("JoinButton")?.GetComponent<Button>();

                if (roomNameText != null) roomNameText.text = lobby.Name;
                if (playerCountText != null) playerCountText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";

                string joinCode = lobby.Data["JOIN_CODE"].Value;
                if (joinButton != null)
                {
                    joinButton.onClick.AddListener(() => { _ = JoinRelayWithCode(joinCode); });
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Oda listesi çekilemedi: {e.Message}");
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

    // Að oturumunun kapatýlmasý ve UI elemanlarýnýn tekrar aktif edilmesi
    private void ResetNetworkAndUI()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SetUIInteractable(true);
    }

    // Arayüz Panelleri Navigasyon Metotlarý
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
        if (roomListPanel != null) roomListPanel.SetActive(false);
    }

    public void OpenRoomListPanel()
    {
        if (settingPanel != null) settingPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (marketPanel != null) marketPanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (roomListPanel != null) roomListPanel.SetActive(true);

        RefreshLobbyList();
    }

    public void OpenMarketPanel()
    {
        if (settingPanel != null) settingPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (marketPanel != null) marketPanel.SetActive(true);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (roomListPanel != null) roomListPanel.SetActive(false);
    }

    public void OpenInventoryPanel()
    {
        if (settingPanel != null) settingPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (marketPanel != null) marketPanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(true);
        if (roomListPanel != null) roomListPanel.SetActive(false);
    }

    public void OpenSetiingPanel()
    {
        if (settingPanel != null) settingPanel.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (marketPanel != null) marketPanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (roomListPanel != null) roomListPanel.SetActive(false);
    }

    public void APIDenBilgileriCek()
    {
        StartCoroutine(KullaniciBilgileriniGetirCoroutine());
    }

    // ASP.NET Core Web API sunucusuna JWT Bearer Token ekleyerek yetkili istek (Authorized Request) atan coroutine
    private IEnumerator KullaniciBilgileriniGetirCoroutine()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(userInfoUrl))
        {
            // SessionManager üzerinden oturum açmýþ kullanýcýnýn JWT Access Token'ý HTTP Header'a enjekte edilir
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
                // DTO dönüþümü ile API cevabýnýn ayrýþtýrýlmasý ve kullanýcý arayüzüne yazýlmasý
                UserInfoDto userInfo = JsonUtility.FromJson<UserInfoDto>(request.downloadHandler.text);

                if (oyuncuAdiText != null) oyuncuAdiText.text = userInfo.userName;
                if (altinText != null) altinText.text = "Altýn: " + userInfo.coins;

                PlayerPrefs.SetString("PlayerName", userInfo.userName);
            }
        }
    }
}