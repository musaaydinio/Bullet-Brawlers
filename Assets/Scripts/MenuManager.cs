using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("UI Elemanlarý")]
    [SerializeField] private Button btnStartHost;
    [SerializeField] private Button btnJoinClient;
    [SerializeField] private TMP_InputField inputFieldIP;

    [Header("Ayarlar")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private ushort defaultPort = 7777;

    private UnityTransport transport;

    private void Start()
    {
        if (NetworkManager.Singleton != null)
        {
            transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
          
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        btnStartHost.onClick.AddListener(OnStartHostClicked);
        btnJoinClient.onClick.AddListener(OnJoinClientClicked);
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
      
        SetUIInteractable(false);

        if (transport != null)
        {
            transport.SetConnectionData("127.0.0.1", defaultPort);
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
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }
       
        SetUIInteractable(true);
    }

    private void SetUIInteractable(bool state)
    {
        if (btnStartHost != null) btnStartHost.interactable = state;
        if (btnJoinClient != null) btnJoinClient.interactable = state;
        if (inputFieldIP != null) inputFieldIP.interactable = state;
    }
}

