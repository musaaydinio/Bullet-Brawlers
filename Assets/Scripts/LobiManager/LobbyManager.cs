using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : NetworkBehaviour
{
    [Header("UI Elemanlarý")]
    [SerializeField] private TextMeshProUGUI oyuncuListesiText;
    [SerializeField] private Button btnHazirVer;
    [SerializeField] private TextMeshProUGUI btnHazirVerText;
    [SerializeField] private Button btnOyunuBaslat; 
    [SerializeField] private TextMeshProUGUI odaKoduText;
    [SerializeField] private Button btnMenuyeDon;

    [Header("Ayarlar")]
    private string gameSceneName = "GameScene";
    private string mainMenuSceneName = "MainMenu";

    // Tüm istemciler (clients) arasýnda otomatik senkronize olan oyuncu listemiz
    private NetworkList<LobbyPlayerData> lobbyPlayers;

    private void Awake()
    {
        // NetworkList nesnesini sunucu ve istemci tarafýnda initialize ediyoruz
        lobbyPlayers = new NetworkList<LobbyPlayerData>();
    }

    public override void OnNetworkSpawn()
    {
        // Liste her deðiþtiðinde UI'ý reaktif (reactive) olarak güncellemek için olaya abone oluyoruz
        lobbyPlayers.OnListChanged += OnLobbyPlayersChanged;

        // Relay / Lobi katýlým kodunu istemci arayüzünde gösteriyoruz
        if (odaKoduText != null && !string.IsNullOrEmpty(LobbyDataHolder.CurrentJoinCode))
        {
            odaKoduText.text = "Oda Kodu: " + LobbyDataHolder.CurrentJoinCode;
        }

        if (IsServer)
        {
            // Sunucu tarafýnda baðlantý ve kopma olaylarýný dinliyoruz
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            // Host (Oda Sahibi) kendisini doðrudan lobi listesine ekler
            string hostName = PlayerPrefs.GetString("PlayerName", "Host_Player");
            lobbyPlayers.Add(new LobbyPlayerData(NetworkManager.Singleton.LocalClientId,hostName,true,true));
        }
        else
        {
            // Ýstemci tarafýnda sunucudan kopma durumunu dinliyoruz
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedClientSide;

            // Ýstemci baðlandýðýnda kendi adýný sunucuya RPC aracýlýðýyla bildirir
            string clientName = PlayerPrefs.GetString("PlayerName", "Oyuncu_" + Random.Range(100, 999));
            RegisterPlayerServerRpc(clientName);
        }

        // UI Buton olaylarýnýn baðlanmasý
        if (btnHazirVer != null) btnHazirVer.onClick.AddListener(OnHazirVerClicked);
        if (btnOyunuBaslat != null) btnOyunuBaslat.onClick.AddListener(OnOyunuBaslatClicked);
        if (btnMenuyeDon != null) btnMenuyeDon.onClick.AddListener(OnMenuyeDonClicked);

        UI_Guncelle();
    }

    public override void OnNetworkDespawn()
    {
        // Bellek sýzýntýlarýný (memory leak) önlemek için að nesnesi yok olurken olay aboneliklerini temizliyoruz
        lobbyPlayers.OnListChanged -= OnLobbyPlayersChanged;

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        // Sunucu yeni client baðlandýðýnda iþlem yapar
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        // Ayrýlan oyuncuyu sunucu tarafýnda NetworkList'ten güvenli þekilde çýkarýyoruz
        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (lobbyPlayers[i].clientId == clientId)
            {
                lobbyPlayers.RemoveAt(i);
                break;
            }
        }             
    }

    private void OnClientDisconnectedClientSide(ulong clientId)
    {
        // Baðlantý kesilirse veya Host odayý kapatýrsa istemciyi ana menüye yönlendiriyoruz
        if (clientId == NetworkManager.Singleton.LocalClientId || clientId == NetworkManager.ServerClientId)
        {
            OnMenuyeDonClicked();
        }
    }

    // Ýstemcinin kendi adýný sunucuya kaydedebilmesi için çaðrý yaptýðý ServerRpc yöntemi.
    [ServerRpc(RequireOwnership = false)]
    private void RegisterPlayerServerRpc(string playerName, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        lobbyPlayers.Add(new LobbyPlayerData(senderId, playerName, false, false));
    }

    private void OnHazirVerClicked()
    {
        ToggleReadyServerRpc();
    }

    // Oyuncunun hazýr olma durumunu sunucu tarafýnda güvenli bir þekilde günceller.
    [ServerRpc(RequireOwnership = false)]
    private void ToggleReadyServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId= rpcParams.Receive.SenderClientId;

        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (lobbyPlayers[i].clientId == senderId)
            {
                LobbyPlayerData data=lobbyPlayers[i];
                data.isReady=!data.isReady;
                lobbyPlayers[i]=data;// NetworkList içinde struct güncellemesi
                break;
            }
        }
    }

    private void OnOyunuBaslatClicked()
    {
        if (!IsServer) return;

        // Tüm oyuncular hazýrsa sunucu üzerinden senkronize sahne geçiþi (Network Scene Management) baþlatýyoruz
        if (HerkesHazirMi())
        {
            NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    private void OnLobbyPlayersChanged(NetworkListEvent<LobbyPlayerData> changeEvent)
    {
        UI_Guncelle();
    }

    // Aðdaki son oyuncu listesi verisine göre lobinin arayüzünü yeniden çizer.
    private void UI_Guncelle()
    {
        // Oyuncu Listesini Güncelle
        if (oyuncuListesiText != null)
        {
            oyuncuListesiText.text = "<b>KATILAN OYUNCULAR</b>\n\n";
            for (int i = 0; i < lobbyPlayers.Count; i++)
            {
                var player = lobbyPlayers[i];
                string unvan = player.isHost ? " <color=yellow>[ODA SAHÝBÝ]</color>" : "";
                string durum = player.isReady ? "<color=green>[HAZIR]</color>" : "<color=red>[BEKLENÝYOR]</color>";

                oyuncuListesiText.text += $"{i + 1}. {player.playerName}{unvan} - {durum}\n";
            }
        }

        // 2. Yerel Oyuncunun Buton Durumlarýný Düzenle
        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            if (lobbyPlayers[i].clientId == NetworkManager.Singleton.LocalClientId)
            {
                if (btnHazirVerText != null)
                {
                    btnHazirVerText.text = lobbyPlayers[i].isReady ? "HAZIR DEÐÝL" : "HAZIR VER";
                }

                // Oda sahibi zaten oyunu baþlatacaðý için ekstra "Hazýr Ver" butonunu gizliyoruz
                if (lobbyPlayers[i].isHost && btnHazirVer != null)
                {
                    btnHazirVer.gameObject.SetActive(false);
                }
                break;
            }
        }

        // Oyunu Baþlat Butonunun Yetkisini ve Aktifliðini Kontrol Et (Sadece Sunucu)
        if (btnOyunuBaslat != null)
        {
            btnOyunuBaslat.gameObject.SetActive(IsServer);
            btnOyunuBaslat.interactable = HerkesHazirMi();
        }
    }

    // Lobideki tüm oyuncularýn hazýr olup olmadýðýný doðrular.
    private bool HerkesHazirMi()
    {
        if (lobbyPlayers.Count == 0) return false;

        foreach (var p in lobbyPlayers)
        {
            if (!p.isReady) return false;
        }
        return true;
    }

    private void OnMenuyeDonClicked()
    {
        // Að dinlemesini güvenli þekilde durdurup yerel veriyi temizliyoruz
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        LobbyDataHolder.CurrentJoinCode = "";

        SceneManager.LoadScene(mainMenuSceneName);
    }
}
